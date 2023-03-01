using Microsoft.Extensions.Logging;
using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.Queues.Behaviours;
using PlatformX.ServiceLayer.Behaviours;
using PlatformX.ServiceLayer.Types;
using PlatformX.Settings.Behaviours;
using System;
using System.Threading.Tasks;

namespace PlatformX.ServiceLayer.Helper
{
    public class MicroServiceQueueHelper<TLog> : ClientHelperBase<TLog>, IMicroServiceQueueHelper
    {
        public MicroServiceQueueHelper(IProtectedConfiguration protectedConfiguration,
            IHashGeneration hashGenerationHelper,
            IQueueXClient queueClient,
            ILogger<TLog> traceLogger) : base(protectedConfiguration, hashGenerationHelper, queueClient, traceLogger)
        {
     
        }
        
        public async Task<TResponse> SubmitQueueMessage<TRequest, TResponse>(TRequest request, 
            RequestContext requestContext, 
            ServiceMetaData serviceMetaData, 
            string queueName,
            string systemApiRoleType = "",
            string regionKey = "",
            string locationKey = "") where TResponse : GenericResponse, new()
        {
            var response = default(TResponse);

            if (string.IsNullOrEmpty(queueName))
            {
                throw new ArgumentNullException(nameof(queueName), $"{nameof(queueName)} not provided");
            }

            if (serviceMetaData == null)
            {
                throw new ArgumentNullException(nameof(serviceMetaData), $"{nameof(serviceMetaData)} is null please check configuration");
            }

            try
            {
                var serviceKeyName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()];
                var serviceSecretName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()];

                response = await SubmitQueueMessage<TRequest, TResponse>(request, 
                    requestContext, 
                    queueName, 
                    serviceKeyName, 
                    serviceSecretName, 
                    systemApiRoleType, 
                    regionKey, 
                    locationKey);

                response.Success = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, queueName);
                _traceLogger.LogWarning(ex.StackTrace, queueName);
                throw;
            }

            return response;
        }
    }
}