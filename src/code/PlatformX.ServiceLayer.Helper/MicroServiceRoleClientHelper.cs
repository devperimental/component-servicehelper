using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlatformX.Http.Behaviours;
using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.ServiceLayer.Behaviours;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using PlatformX.Settings.Shared.Behaviours;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;

namespace PlatformX.ServiceLayer.Helper
{
    public class MicroServiceRoleClientHelper<TLog> : IMicroServiceRoleClientHelper
    {
        private readonly IProtectedRoleConfiguration _protectedRoleConfiguration;
        private readonly IHashGeneration _hashGenerationHelper;
        private readonly IHttpRequestHelper _httpRequestHelper;
        private readonly ILogger<TLog> _traceLogger;

        public MicroServiceRoleClientHelper(IProtectedRoleConfiguration protectedRoleConfiguration,
            IHttpRequestHelper httpRequestHelper,
            IHashGeneration hashGenerationHelper,
            ILogger<TLog> traceLogger)
        {
            _protectedRoleConfiguration = protectedRoleConfiguration ?? throw new ArgumentNullException(nameof(protectedRoleConfiguration), $"{nameof(protectedRoleConfiguration)} is null in MicroServiceClientHelper");
            _httpRequestHelper = httpRequestHelper ?? throw new ArgumentNullException(nameof(httpRequestHelper), $"{nameof(httpRequestHelper)} is null in MicroServiceClientHelper");
            _hashGenerationHelper = hashGenerationHelper ?? throw new ArgumentNullException(nameof(hashGenerationHelper), $"{nameof(hashGenerationHelper)} is null in MicroServiceClientHelper");
            _traceLogger = traceLogger ?? throw new ArgumentNullException(nameof(traceLogger), $"{nameof(traceLogger)} is null in MicroServiceClientHelper");
        }
    
        public TResponse SubmitRequest<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, string actionName, string envKey, string portNumber, string regionKey, string locationKey, string fulfilmentRoleType) where TResponse : GenericResponse
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
                    response = AzfSubmitRequestHTTP<TRequest, TResponse>(request, requestContext, serviceMetaData, operation, envKey, portNumber, regionKey, locationKey, fulfilmentRoleType);
                }
            }

            return response;
        }

        private TResponse AzfSubmitRequestHTTP<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, ServiceOperation operation, string envKey, string portNumber, string regionKey, string locationKey, string fulfilmentRoleType)
        {
            // Need to generate the endpoint uri based on region and location key
            // Need to know which service is being passed in

            // identity-clnt-au-est take code from the meta data generator
            // in the RoleClientServiceWrapper need to pass in region and location as parameters
            TResponse response;
            var uri = FormatAzfUri(serviceMetaData.Name, envKey, portNumber, regionKey, fulfilmentRoleType);
            try
            {
                var headers = CreateHeaders(requestContext, serviceMetaData, fulfilmentRoleType, regionKey, locationKey);
                var data = JsonConvert.SerializeObject(request);
                
                if (operation.Parameters["AUTHORIZE"] == "true")
                {
                    var accessKey = _protectedRoleConfiguration.GetSecretString(serviceMetaData.Endpoints[0].AccessKey, fulfilmentRoleType, regionKey, locationKey);
                    uri = string.Format(uri, operation.Name, accessKey);
                }
                else
                {
                    uri = string.Format(uri, operation.Name);
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

        private string FormatAzfUri(string serviceName, string envKey, string portNumber, string regionKey, string fulfilmentRoleType)
        {
            if (envKey == "local")
            {
                return $"http://localhost:{portNumber}/api/" + "{0}";
            }
            else
            {
                var roleKey = fulfilmentRoleType == "M" ? "mgmt" : "clnt";
                return $"https://dz-func-{envKey.ToLower()}-{roleKey.ToLower()}-{regionKey.ToLower()}-{serviceName.ToLower()}service.azurewebsites.net/api/" + "{0}?code={1}";
            }
        }

        private Dictionary<string, string> CreateHeaders(RequestContext requestContext, ServiceMetaData serviceMetaData, string fulfilmentRoleType, string regionKey, string locationKey)
        {
            var timestamp = DateTime.Now.Ticks.ToString();

            var serviceKeyName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()];
            var requestServiceKey = _protectedRoleConfiguration.GetSecretString(serviceKeyName, fulfilmentRoleType, regionKey, locationKey);

            var serviceSecretKeyName = serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()];
            var requestServiceSecret = _protectedRoleConfiguration.GetSecretString(serviceSecretKeyName, fulfilmentRoleType, regionKey, locationKey);

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