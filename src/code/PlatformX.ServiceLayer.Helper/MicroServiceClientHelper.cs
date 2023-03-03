using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlatformX.Http.Behaviours;
using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.Queues.Behaviours;
using PlatformX.ServiceLayer.Behaviours;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using PlatformX.Settings.Shared.Behaviours;
using PlatformX.Storage.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlatformX.ServiceLayer.Helper
{
    public class MicroServiceClientHelper<TLog> : IMicroServiceClientHelper
    {
        private readonly IProtectedConfiguration _protectedConfiguration;
        private readonly IHashGeneration _hashGenerationHelper;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly ILogger<TLog> _traceLogger;
        private IQueueXClient _queueClient;
        private readonly IFileStore _fileStore;

        public MicroServiceClientHelper(IProtectedConfiguration protectedConfiguration,
            IHttpRequestHelper httpRequestHelper,
            IQueueXClient queueClient,
            IHashGeneration hashGenerationHelper,
            ILogger<TLog> traceLogger,
            IFileStore fileStore)
        {
            _protectedConfiguration = protectedConfiguration ?? throw new ArgumentNullException(nameof(protectedConfiguration), $"{nameof(protectedConfiguration)} is null in MicroServiceClientHelper");
            _httpRequestHelper = httpRequestHelper ?? throw new ArgumentNullException(nameof(httpRequestHelper), $"{nameof(httpRequestHelper)} is null in MicroServiceClientHelper");
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient), $"{nameof(queueClient)} is null in MicroServiceClientHelper");
            _hashGenerationHelper = hashGenerationHelper ?? throw new ArgumentNullException(nameof(hashGenerationHelper), $"{nameof(hashGenerationHelper)} is null in MicroServiceClientHelper");
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger), $"{nameof(traceLogger)} is null in MicroServiceClientHelper");
            _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore), $"{nameof(fileStore)} is null in MicroServiceClientHelper");
        }
    
        public TResponse SubmitRequest<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, string actionName) where TResponse : GenericResponse
        {
            if (string.IsNullOrEmpty(actionName))
            {
                throw new ArgumentNullException(nameof(actionName), $"{nameof(actionName)} is null in {nameof(SubmitRequest)}");
            }

            if (requestContext == null)
            {
                throw new ArgumentNullException(nameof(requestContext), $"{nameof(requestContext)} is null in {nameof(SubmitRequest)}");
            }

            if (request == null)
            {
                throw new ArgumentNullException(nameof(request), $"{nameof(request)} is null in {nameof(SubmitRequest)}");
            }

            if (serviceMetaData == null)
            {
                throw new ArgumentNullException(nameof(serviceMetaData), $"{nameof(serviceMetaData)} is null in {nameof(SubmitRequest)}");
            }

            var endpoint = serviceMetaData.Endpoints.SingleOrDefault(c => c.IsPrimary);

            if (endpoint == null)
            {
                throw new ArgumentNullException(nameof(endpoint), $"{nameof(endpoint)} is null for serviceName {serviceMetaData.Name}");
            }

            var operation = endpoint.Operations.SingleOrDefault(c => c.Name == actionName);

            if (operation == null)
            {
                throw new ArgumentNullException(nameof(operation), $"{nameof(operation)} is null for serviceName {serviceMetaData.Name} and actionName:{actionName}");
            }

            var response = default(TResponse);

            if (endpoint.Type == ServiceEndpointType.AZF)
            {
                if (operation.Protocol == ServiceEndpointProtocol.HTTP)
                {
                    if (operation.Cache != null && operation.Cache.Enabled)
                    {
                        response = TryRetrieveFile<TRequest, TResponse>(request, serviceMetaData.Name, serviceMetaData, operation);

                        if ((response != default(TResponse) && response.Success) || operation.Cache.ReturnOnNull)
                        {
                            return response;
                        }
                    }

                    response = AzfSubmitRequestHTTP<TRequest, TResponse>(request, requestContext, serviceMetaData, operation);

                    if (operation.Cache != null && operation.Cache.Enabled)
                    {
                        TrySaveFile(request, response, serviceMetaData.Name, serviceMetaData, operation);
                    }
                }
                else if (operation.Protocol == ServiceEndpointProtocol.QUEUE)
                {
                    response = (TResponse)AzfSubmitRequestQueue<TRequest, GenericResponse>(request, requestContext, serviceMetaData, operation).Result;
                }
            }

            return response;
        }

        private TResponse TryRetrieveFile<TRequest, TResponse>(TRequest request, string serviceName, ServiceMetaData serviceMetaData, ServiceOperation operation)
        {
            var filePath = GenerateFileName(request, serviceName, operation);

            var roleTypeSetting = GetFulfillmentRoleTypeSettings(serviceMetaData);

            _traceLogger.LogInformation($"TryRetrieveFile: {serviceName}|{roleTypeSetting.RoleKey}|{roleTypeSetting.RegionKey}|{roleTypeSetting.LocationKey}|{filePath}");
            
            return _fileStore.LoadFile<TResponse>(serviceName, roleTypeSetting.RoleKey, roleTypeSetting.RegionKey, roleTypeSetting.LocationKey, filePath);
        }

        private void TrySaveFile<TRequest, TResponse>(TRequest request, TResponse data, string serviceName, ServiceMetaData serviceMetaData, ServiceOperation operation)
        {
            var filePath = GenerateFileName<TRequest>(request, serviceName, operation);
            var contentType = "text/plain";

            var roleTypeSetting = GetFulfillmentRoleTypeSettings(serviceMetaData);

            _traceLogger.LogInformation($"TrySaveFile: {serviceName}|{roleTypeSetting.RoleKey}|{roleTypeSetting.RegionKey}|{roleTypeSetting.LocationKey}|{filePath}");
            
            _fileStore.SaveFile(data, serviceName, roleTypeSetting.RoleKey, roleTypeSetting.RegionKey, roleTypeSetting.LocationKey, filePath, contentType);
        }

        private RoleTypeSetting GetFulfillmentRoleTypeSettings(ServiceMetaData serviceMetaData)
        {
            var fulfillmentRoleType = serviceMetaData.Endpoints[0].FulfillmentRoleType;
            if (fulfillmentRoleType == null)
            {
                throw new ArgumentNullException(nameof(fulfillmentRoleType), $"For service {serviceMetaData.Name}");
            }

            var roleTypeSetting = serviceMetaData.RoleTypeSettings[fulfillmentRoleType];

            if (roleTypeSetting == null)
            {
                throw new ArgumentNullException(nameof(roleTypeSetting), $"For service {serviceMetaData.Name}");
            }

            return roleTypeSetting;
        }

        private string GenerateFileName<TRequest>(TRequest request, string serviceName, ServiceOperation operation)
        {
            string filePath;
            if (operation.Cache.PerRequest)
            {
                var cacheKey = request.GetType().GetProperty(operation.Cache.PerRequestIdentifier)?.GetValue(request);
                var hashValue = _hashGenerationHelper.CreateHash((string)cacheKey, HashType.SHA256);
                filePath = operation.Cache.FileKey;
                filePath = filePath.Replace("{MethodName}", operation.Name.ToLower());
                filePath = filePath.Replace("{Id}", hashValue.ToLower());
            }
            else
            {
                filePath = operation.Cache.FileKey;
                filePath = filePath.Replace("{MethodName}", operation.Name.ToLower());
            }

            return filePath;
        }

        private TResponse AzfSubmitRequestHTTP<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, ServiceOperation operation)
        {
            TResponse response;
            var uri = string.Empty;
            try
            {
                var headers = CreateHeaders(requestContext, serviceMetaData);
                var data = JsonConvert.SerializeObject(request);
                
                if (operation.Parameters["AUTHORIZE"] == "true")
                {
                    uri = string.Format(serviceMetaData.Endpoints[0].Uri, operation.Name, _protectedConfiguration.GetSecretString(serviceMetaData.Endpoints[0].AccessKey));
                }
                else
                {
                    uri = string.Format(serviceMetaData.Endpoints[0].Uri, operation.Name);
                }

                var method = new HttpMethod(operation.Parameters["METHOD"]);
                
                response = _httpRequestHelper.SubmitRequest<TResponse>(uri, method, data, headers).Result;
            }
            catch (UriFormatException exA)
            {
                _traceLogger.LogError(exA, $"operationName:{operation.Name} - uri:{uri}");
                throw;
            }
            catch (Exception exB)
            {
                _traceLogger.LogError(exB, operation.Name);
                throw;
            }

            return response;
        }

        private async Task<TResponse> AzfSubmitRequestQueue<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, ServiceOperation operation) where TResponse : GenericResponse, new()
        {
            var response = default(TResponse);

            if (!operation.Parameters.ContainsKey("QUEUENAME"))
            {
                throw new ArgumentNullException("QUEUENAME", $"QUEUENAME not specified for operation: {operation.Name}");
            }

            if (_queueClient == null)
            {
                throw new ArgumentNullException(nameof(_queueClient), $"Queue not initialised for {serviceMetaData.Name}");
            }

            try
            {
                response = new TResponse();
                var headers = CreateHeaders(requestContext, serviceMetaData);
                var data = JsonConvert.SerializeObject(request);
                var messageId = Guid.NewGuid().ToString();
                var queueName = operation.Parameters["QUEUENAME"];
                var deferSeconds = 0;
                
                if (operation.Parameters.ContainsKey("DEFER_SECONDS"))
                {
                    int.TryParse(operation.Parameters["DEFER_SECONDS"], out deferSeconds);
                }

                var roleTypeSetting = GetFulfillmentRoleTypeSettings(serviceMetaData);

                _traceLogger.LogInformation($"AzfSubmitRequestQueue: {queueName}|{roleTypeSetting.RoleKey}|{roleTypeSetting.RegionKey}|{roleTypeSetting.LocationKey}");

                if (queueName == "authorize-record")
                {
                    _traceLogger.LogInformation($"deferSeconds: {deferSeconds}, operation data: {JsonConvert.SerializeObject(operation)}");
                }

                await _queueClient.SendMessage(data, headers, messageId, roleTypeSetting.RoleKey, roleTypeSetting.RegionKey, roleTypeSetting.LocationKey, queueName, deferSeconds);

                response.MessageId = messageId;
                response.Success = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, operation.Name);
                throw;
            }

            return response;
        }

        private Dictionary<string, string> CreateHeaders(RequestContext requestContext, ServiceMetaData serviceMetaData)
        {
            var timestamp = DateTime.Now.Ticks.ToString();

            var serviceKeyName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()];
            var requestServiceKey = _protectedConfiguration.GetSecretString(serviceKeyName);

            var serviceSecretKeyName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()];
            var requestServiceSecret = _protectedConfiguration.GetSecretString(serviceSecretKeyName);

            if (string.IsNullOrEmpty(requestServiceKey))
            {
                _traceLogger.LogWarning($"Key not found:{serviceKeyName}");
                throw new ArgumentNullException(nameof(requestServiceKey));
            }

            if (string.IsNullOrEmpty(requestServiceSecret))
            {
                _traceLogger.LogWarning($"Key not found:{serviceSecretKeyName}");
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
                { ServiceHeaderConstants.UserGlobalId, requestContext.UserGlobalId},
                { ServiceHeaderConstants.OrganisationGlobalId, requestContext.OrganisationGlobalId},
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