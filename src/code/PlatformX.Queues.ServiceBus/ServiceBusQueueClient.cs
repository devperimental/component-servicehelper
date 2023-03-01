using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using PlatformX.Queues.Behaviours;
using Polly;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PlatformX.Common.Types.DataContract;
using PlatformX.Settings.Behaviours;
using PlatformX.Messaging.Types.Constants;

namespace PlatformX.Queues.ServiceBus
{
    public class ServiceBusQueueClient<TLog> : IQueueXClient
    {
        private readonly Dictionary<string, QueueClient> _queueClient;
        private readonly IEndpointHelper _endpointHelper;
        private readonly BootstrapConfiguration _bootstrapConfiguration;
        private readonly ILogger<TLog> _traceLogger;
        private readonly object _lockMe = new object();

        public ServiceBusQueueClient(ILogger<TLog> traceLogger,
            IEndpointHelper endpointHelper,
            BootstrapConfiguration bootstrapConfiguration)
        {
            _traceLogger = traceLogger;
            _endpointHelper = endpointHelper;
            _bootstrapConfiguration = bootstrapConfiguration;
            _queueClient = new Dictionary<string, QueueClient>();
        }

        private QueueClient GetQueue(string queueName, string serviceBusNamespace)
        {
            try
            {
                if (string.IsNullOrEmpty(queueName))
                {
                    throw new ArgumentNullException("queueName", "queueName is null in GetQueue");
                }

                var queueKey = queueName + serviceBusNamespace;
                lock (_lockMe)
                {
                    if (_queueClient.ContainsKey(queueKey) && !_queueClient[queueKey].IsClosedOrClosing)
                    {
                        return _queueClient[queueKey];
                    }

                    if (_queueClient.ContainsKey(queueKey) && !_queueClient[queueKey].IsClosedOrClosing)
                        return _queueClient[queueKey];
                        
                    var endpoint = serviceBusNamespace + ".servicebus.windows.net";
                    var tokenProvider = new ServiceBusTokenProvider(_bootstrapConfiguration.TenantId);
                    _queueClient[queueKey] = new QueueClient(endpoint, queueName, tokenProvider);
                }
                return _queueClient[queueKey];
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error calling GetQueue for queue:{queueName} and serviceBus:{serviceBusNamespace}");
                throw;
            }
        }

        public async Task SendManagementMessage(string data, Dictionary<string, string> headers, string messageId, string queueName, string regionKey, string locationKey)
        {
            var serviceBusNamespace = _endpointHelper.GetManagementServiceBusNamespace(regionKey, locationKey);
            await SendMessageInternal(data, headers, messageId, serviceBusNamespace, queueName, 0);
        }

        public async Task SendClientMessage(string data, Dictionary<string, string> headers, string messageId, string queueName, string regionKey, string locationKey)
        {
            var serviceBusNamespace = _endpointHelper.GetClientServiceBusNamespace(regionKey, locationKey);
            await SendMessageInternal(data, headers, messageId, serviceBusNamespace, queueName, 0);
        }

        public async Task SendMessage(string data, Dictionary<string, string> headers, string messageId, string roleKey, string regionKey, string locationKey, string queueName)
        {
            await SendMessage(data, headers, messageId, roleKey, regionKey, locationKey, queueName, 0);
        }

        public async Task SendMessage(string data, Dictionary<string, string> headers, string messageId, string roleKey, string regionKey, string locationKey, string queueName, int deferSeconds)
        {
            string serviceBusNamespace = string.Empty;

            if (roleKey == SystemRoleKey.Management)
            {
                serviceBusNamespace = _endpointHelper.GetManagementServiceBusNamespace(regionKey, locationKey);
            }
            else if (roleKey == SystemRoleKey.Client)
            {
                serviceBusNamespace = _endpointHelper.GetClientServiceBusNamespace(regionKey, locationKey);
            }
            else
            {
                serviceBusNamespace = _endpointHelper.GetServiceBusNamespace();
            }

            await SendMessageInternal(data, headers, messageId, serviceBusNamespace, queueName, deferSeconds);
        }

        private async Task SendMessageInternal(string data, Dictionary<string, string> headers, string messageId, string serviceBusNamespace, string queueName, int deferSeconds)
        {
            try
            {
                var policy = Policy
                       .Handle<Exception>()
                       .WaitAndRetryAsync(3,
                           retryAttempt => TimeSpan.FromMilliseconds(200),
                               (exception, timeSpan, retryCount, context) =>
                               {
                                   var msg = $"SendMessage - Count:{retryCount}, Exception:{exception.Message}, Queue Name:{queueName}";
                                   _traceLogger.LogWarning(msg);
                               });

                await policy.ExecuteAsync(() => QueueMessage(data, headers, messageId, serviceBusNamespace, queueName, deferSeconds));

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error in SendMessage queue:{queueName}");
                throw;
            }
        }

        private async Task QueueMessage(string data, Dictionary<string, string> headers, string messageId, string serviceBusNamespace, string queueName, int deferSeconds)
        {
            if (_bootstrapConfiguration.LogMessages)
            {
                _traceLogger.LogInformation($"Begin sendMessage queue:{queueName} and namespace:{serviceBusNamespace}");
            }

            var queueClient = GetQueue(queueName, serviceBusNamespace);

            var brokeredMessage = new Message(Encoding.UTF8.GetBytes(data))
            {
                ContentType = "application/json",  // JSON data
                MessageId = messageId
            };

            foreach(var header in headers)
            {
                brokeredMessage.UserProperties.Add(header.Key, header.Value);
            }

            if (deferSeconds > 0)
            {
                var scheduleTime = DateTime.UtcNow.AddSeconds(deferSeconds);
                _traceLogger.LogInformation($"scheduleTime is :{scheduleTime}");

                brokeredMessage.ScheduledEnqueueTimeUtc = scheduleTime;
                await queueClient.ScheduleMessageAsync(brokeredMessage, new DateTimeOffset(scheduleTime));
            }
            else
            {
                await queueClient.SendAsync(brokeredMessage);
            }

            
            

            if (_bootstrapConfiguration.LogMessages)
            {
                _traceLogger.LogInformation($"End sendMessage queue:{queueName} and namespace:{serviceBusNamespace}");
            }
        }
    }
}
