using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Queues.Behaviours;
using PlatformX.Settings.Behaviours;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlatformX.ServiceLayer.Helper
{
    public class ClientHelperBase<TLog>
    {
        protected IProtectedConfiguration ProtectedConfiguration;
        private IHashGeneration _hashGenerationHelper;
        protected ILogger<TLog> _traceLogger;
        private readonly IQueueXClient _queueClient;

        public ClientHelperBase(IProtectedConfiguration protectedConfiguration,
            IHashGeneration hashGenerationHelper,
            IQueueXClient queueClient,
            ILogger<TLog> traceLogger)
        {
            ProtectedConfiguration = protectedConfiguration;
            _hashGenerationHelper = hashGenerationHelper;
            _queueClient = queueClient;
            _traceLogger = traceLogger;

            if (protectedConfiguration == null)
            {
                throw new ArgumentNullException(nameof(protectedConfiguration), $"{nameof(protectedConfiguration)} is null in MicroServiceClientHelper");
            }

            if (_hashGenerationHelper == null)
            {
                throw new ArgumentNullException(nameof(_hashGenerationHelper), $"{nameof(_hashGenerationHelper)} is null in MicroServiceClientHelper");
            }

            if (queueClient == null)
            {
                throw new ArgumentNullException(nameof(queueClient), $"{nameof(queueClient)} is null in MicroServiceClientHelper");
            }

            if (traceLogger == null)
            {
                throw new ArgumentNullException(nameof(traceLogger), $"{nameof(traceLogger)} is null in MicroServiceClientHelper");
            }
        }

        public async Task<TResponse> SubmitQueueMessage<TRequest, TResponse>(TRequest request,
            RequestContext requestContext,
            string queueName,
            string serviceKeyName,
            string serviceSecretName,
            string roleKey,
            string regionKey,
            string locationKey) where TResponse : GenericResponse, new()
        {
            var response = default(TResponse);

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName), $"{nameof(queueName)} not specified for operation: {queueName}");
            }

            try
            {
                response = new TResponse();
                var headers = CreateHeaders(requestContext, serviceKeyName, serviceSecretName);
                var data = JsonConvert.SerializeObject(request);
                var messageId = Guid.NewGuid().ToString();

                await _queueClient.SendMessage(data, headers, messageId, roleKey, regionKey, locationKey, queueName);

                response.MessageId = messageId;
                response.Success = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, queueName);
                throw;
            }

            return response;
        }

        private Dictionary<string, string> CreateHeaders(RequestContext requestContext, string serviceKeyName, string serviceSecretName)
        {
            var timestamp = DateTime.Now.Ticks.ToString();

            var requestServiceKey = ProtectedConfiguration.GetSecretString(serviceKeyName);
            var requestServiceSecret = ProtectedConfiguration.GetSecretString(serviceSecretName);

            if (string.IsNullOrEmpty(requestServiceKey))
            {
                _traceLogger.LogWarning($"Key not found:{serviceKeyName}");
                throw new ArgumentNullException(nameof(requestServiceKey));
            }

            if (string.IsNullOrEmpty(requestServiceSecret))
            {
                _traceLogger.LogWarning($"Key not found:{serviceSecretName}");
                throw new ArgumentNullException(nameof(requestServiceSecret));
            }

            var requestHash = _hashGenerationHelper.GenerateRequestInput(requestServiceKey, requestServiceSecret, timestamp, requestContext.IpAddress, requestContext.CorrelationId);

            var headers = new Dictionary<string, string>
            {
                { ServiceHeaderConstants.RequestServiceHash, requestHash},
                { ServiceHeaderConstants.PortalName, requestContext.PortalName},
                { ServiceHeaderConstants.RequestServiceKey, requestServiceKey},
                { ServiceHeaderConstants.RequestServiceTimestamp, timestamp},
                { ServiceHeaderConstants.IpAddress, requestContext.IpAddress},
                { ServiceHeaderConstants.CorrelationId, requestContext.CorrelationId},
                { ServiceHeaderConstants.SessionId, requestContext.SessionId},
                { ServiceHeaderConstants.IdentityId, requestContext.IdentityId},
                { ServiceHeaderConstants.SystemApiRoleType, requestContext.SystemApiRoleType}
            };

            if (requestContext.SystemApiRoleType == SystemApiRoleType.Client)
            {
                headers.Add(ServiceHeaderConstants.ClientApplicationKey, requestContext.ClientApplicationKey);
                headers.Add(ServiceHeaderConstants.ClientApiKey, requestContext.ClientApiKey);
                headers.Add(ServiceHeaderConstants.ClientApplicationGlobalId, requestContext.ClientApplicationGlobalId);
            }

            headers.Add(ServiceHeaderConstants.ClientAppEnvironment, requestContext.ClientAppEnvironment);

            return headers;
        }
    }
}
