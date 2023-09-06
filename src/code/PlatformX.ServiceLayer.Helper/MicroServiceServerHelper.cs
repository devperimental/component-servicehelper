using Microsoft.Extensions.Logging;
using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.ServiceLayer.Behaviours;
using PlatformX.Settings.Shared.Behaviours;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;

namespace PlatformX.ServiceLayer.Helper
{
    public class MicroServiceServerHelper : IMicroServiceServerHelper
    {
        private readonly IPortalSettings _portalSettings;
        private readonly IHashGeneration _hashGenerationHelper;
        private readonly Dictionary<string, string> _serviceKeys;

        public MicroServiceServerHelper(IPortalSettings portalSettings, IHashGeneration hashGenerationHelper, Dictionary<string,string> serviceKeys)
        {
            _portalSettings = portalSettings;
            _hashGenerationHelper = hashGenerationHelper;
            _serviceKeys = serviceKeys;

            if (portalSettings == null)
            {
                throw new ArgumentNullException(nameof(portalSettings), $"{nameof(portalSettings)} is null in {nameof(MicroServiceServerHelper)}");
            }

            if (hashGenerationHelper == null)
            {
                throw new ArgumentNullException(nameof(hashGenerationHelper), $"{nameof(hashGenerationHelper)} is null in {nameof(MicroServiceServerHelper)}");
            }

            if (serviceKeys == null)
            {
                throw new ArgumentNullException(nameof(serviceKeys), $"{nameof(serviceKeys)} is null in {nameof(MicroServiceServerHelper)}");
            }
        }

        public RequestContext GetRequestContext<T>(HttpRequestMessage req, ILogger<T> traceLogger, bool bypassHashCheck = false)
        {
            var context = new RequestContext();

            try
            {
                var serviceHeader = ParseServiceHeader(req);
                context = GetRequestContext(serviceHeader, traceLogger, bypassHashCheck);
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                context.ResponseContent += ex.Message;
                context.ResponseCode = (int)HttpStatusCode.ServiceUnavailable;
            }

            return context;
        }

        public RequestContext GetRequestContext<T>(IDictionary<string, object> userProperties, ILogger<T> traceLogger, bool bypassHashCheck = false)
        {
            var context = new RequestContext();

            try
            {
                var serviceHeader = ParseServiceHeader(userProperties);
                context = GetRequestContext(serviceHeader, traceLogger, bypassHashCheck);
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                context.ResponseContent += ex.Message;
                context.ResponseCode = (int)HttpStatusCode.ServiceUnavailable;
            }

            return context;
        }

        #region Private Methods
        private ServiceHeader ParseServiceHeader(HttpRequestMessage req)
        {
            var serviceHeader = new ServiceHeader
            {
                RequestServiceHash = ExtractHeaderValue(req, ServiceHeaderConstants.RequestServiceHash),
                PortalName = ExtractHeaderValue(req, ServiceHeaderConstants.PortalName),
                RequestServiceKey = ExtractHeaderValue(req, ServiceHeaderConstants.RequestServiceKey),
                RequestServiceTimestamp = ExtractHeaderValue(req, ServiceHeaderConstants.RequestServiceTimestamp),
                IpAddress = ExtractHeaderValue(req, ServiceHeaderConstants.IpAddress),
                CorrelationId = ExtractHeaderValue(req, ServiceHeaderConstants.CorrelationId),
                SessionId = ExtractHeaderValue(req, ServiceHeaderConstants.SessionId),
                IdentityId = ExtractHeaderValue(req, ServiceHeaderConstants.IdentityId),
                OrganisationGlobalId = ExtractHeaderValue(req, ServiceHeaderConstants.OrganisationGlobalId),
                UserGlobalId = ExtractHeaderValue(req, ServiceHeaderConstants.UserGlobalId),
                SystemApiRoleType = ExtractHeaderValue(req, ServiceHeaderConstants.SystemApiRoleType),
                ClientApplicationKey = ExtractHeaderValue(req, ServiceHeaderConstants.ClientApplicationKey),
                ClientApiKey = ExtractHeaderValue(req, ServiceHeaderConstants.ClientApiKey),
                ClientApplicationGlobalId = ExtractHeaderValue(req, ServiceHeaderConstants.ClientApplicationGlobalId),
                ClientAppEnvironment = ExtractHeaderValue(req, ServiceHeaderConstants.ClientAppEnvironment)
            };

            if (string.IsNullOrEmpty(serviceHeader.SystemApiRoleType))
            {
                throw new ArgumentNullException("SystemApiRoleType must be specified in the service header");
            }

            if (serviceHeader.SystemApiRoleType == SystemApiRoleType.Client && string.IsNullOrEmpty(serviceHeader.ClientAppEnvironment))
            {
                throw new ArgumentNullException("ClientAppEnvironment must be specified for SystemApiRoleType [Client]");
            }

            return serviceHeader;
        }

        private ServiceHeader ParseServiceHeader(IDictionary<string, object> userProperties)
        {
            var serviceHeader = new ServiceHeader
            {
                RequestServiceHash = userProperties[ServiceHeaderConstants.RequestServiceHash] as string,
                PortalName = userProperties[ServiceHeaderConstants.PortalName] as string,
                RequestServiceKey = userProperties[ServiceHeaderConstants.RequestServiceKey] as string,
                RequestServiceTimestamp = userProperties[ServiceHeaderConstants.RequestServiceTimestamp] as string,
                IpAddress = userProperties[ServiceHeaderConstants.IpAddress] as string,
                CorrelationId = userProperties[ServiceHeaderConstants.CorrelationId] as string
            };
            
            if (userProperties.ContainsKey(ServiceHeaderConstants.SessionId))
            {
                serviceHeader.SessionId = userProperties[ServiceHeaderConstants.SessionId] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.IdentityId))
            {
                serviceHeader.IdentityId = userProperties[ServiceHeaderConstants.IdentityId] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.OrganisationGlobalId))
            {
                serviceHeader.OrganisationGlobalId = userProperties[ServiceHeaderConstants.OrganisationGlobalId] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.UserGlobalId))
            {
                serviceHeader.UserGlobalId = userProperties[ServiceHeaderConstants.UserGlobalId] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.SystemApiRoleType))
            {
                serviceHeader.SystemApiRoleType = userProperties[ServiceHeaderConstants.SystemApiRoleType] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.ClientApplicationKey))
            {
                serviceHeader.ClientApplicationKey = userProperties[ServiceHeaderConstants.ClientApplicationKey] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.ClientApiKey))
            {
                serviceHeader.ClientApiKey = userProperties[ServiceHeaderConstants.ClientApiKey] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.ClientApplicationGlobalId))
            {
                serviceHeader.ClientApplicationGlobalId = userProperties[ServiceHeaderConstants.ClientApplicationGlobalId] as string;
            }

            if (userProperties.ContainsKey(ServiceHeaderConstants.ClientAppEnvironment))
            {
                serviceHeader.ClientAppEnvironment = userProperties[ServiceHeaderConstants.ClientAppEnvironment] as string;
            }

            return serviceHeader;
        }

        private RequestContext GetRequestContext<T>(ServiceHeader serviceHeader, ILogger<T> traceLogger, bool bypassHashCheck = false)
        {
            var context = new RequestContext();

            try
            {
                if (!bypassHashCheck)
                {
                    var requestServiceSecret = _portalSettings.GetSecretString(_serviceKeys[ServiceConfigurationKeyType.ServiceSecret.ToString()]);

                    if (string.IsNullOrEmpty(serviceHeader.RequestServiceKey))
                    {
                        throw new ArgumentNullException(nameof(serviceHeader.RequestServiceKey));
                    }

                    if (string.IsNullOrEmpty(requestServiceSecret))
                    {
                        throw new ArgumentNullException(nameof(requestServiceSecret));
                    }

                    var generatedHash = _hashGenerationHelper.GenerateRequestInput(serviceHeader.RequestServiceKey, requestServiceSecret, serviceHeader.RequestServiceTimestamp, serviceHeader.IpAddress, serviceHeader.CorrelationId);

                    if (generatedHash != serviceHeader.RequestServiceHash)
                    {
                        traceLogger.LogWarning($"requestServiceHash={serviceHeader.RequestServiceHash}");
                        traceLogger.LogWarning($"generatedHash={generatedHash}");
                        traceLogger.LogInformation("-------------------------------------------------");

                        traceLogger.LogWarning($"portalName={serviceHeader.PortalName}");
                        traceLogger.LogWarning($"requestServiceTimestamp={serviceHeader.RequestServiceTimestamp}");
                        traceLogger.LogWarning($"ipAddress={serviceHeader.IpAddress}");
                        traceLogger.LogWarning($"correlationId={serviceHeader.CorrelationId}");

                        context.ResponseCode = (int)HttpStatusCode.PreconditionFailed;
                        context.ResponseContent += "Exiting call - invalid hash encountered";
                        return context;
                    }

                    if (IsRequestExpired(serviceHeader.RequestServiceTimestamp))
                    {
                        context.ResponseCode = (int)HttpStatusCode.Forbidden;
                        return context;
                    }
                }

                context = new RequestContext
                {
                    PortalName = serviceHeader.PortalName,
                    CorrelationId = serviceHeader.CorrelationId,
                    IpAddress = serviceHeader.IpAddress,
                    SessionId = serviceHeader.SessionId,
                    IdentityId = serviceHeader.IdentityId,
                    OrganisationGlobalId = serviceHeader.OrganisationGlobalId,
                    UserGlobalId = serviceHeader.UserGlobalId,
                    PlanGlobalId = serviceHeader.PlanGlobalId,
                    ActiveSubscription = serviceHeader.ActiveSubscription,
                    SourceTypeKey = serviceHeader.SystemApiRoleType,
                    ResponseCode = (int)HttpStatusCode.OK,
                    ResponseContent = context.ResponseContent
                };

                if (context.SourceTypeValue == SystemApiRoleType.Client)
                {
                    context.ClientApplicationKey = serviceHeader.ClientApplicationKey;
                    context.ClientApiKey = serviceHeader.ClientApiKey;
                    context.ClientApplicationGlobalId = serviceHeader.ClientApplicationGlobalId;
                    context.ClientAppEnvironment = serviceHeader.ClientAppEnvironment;
                }
                else if (context.SourceTypeValue == SystemApiRoleType.Management)
                {
                    context.ClientAppEnvironment = "MGMT";
                }
                else if (context.SourceTypeValue == SystemApiRoleType.Portal)
                {
                    context.ClientAppEnvironment = "PRTL";
                }
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                context.ResponseContent += ex.Message;
                context.ResponseCode = (int)HttpStatusCode.ServiceUnavailable;
            }

            return context;
        }

        private bool IsRequestExpired(string requestTimestamp)
        {
            var checkTimestamp = _portalSettings.GetBool(ServiceConfigurationKeyType.CheckTimestamp.ToString());

            if (!checkTimestamp)
            {
                return false;
            }

            if (!long.TryParse(requestTimestamp, out long requestTicks))
            {
                return true;
            }

            var currentTicks = DateTime.UtcNow.Ticks;
            var difference = currentTicks - requestTicks;

            var seconds = difference / TimeSpan.TicksPerSecond;

            int? expiryTime = _portalSettings.GetInt(ServiceConfigurationKeyType.CallExpirySeconds.ToString());

            // return False if the setting doesn't exist
            return seconds > expiryTime.Value;
        }

        private string ExtractHeaderValue(HttpRequestMessage req, string headerName)
        {
            var headerValue = string.Empty;

            try
            {
                var clientServiceHash = req.Headers.GetValues(headerName);
                using (var headerEnumerator = clientServiceHash.GetEnumerator())
                {
                    while (headerEnumerator.MoveNext())
                    {
                        headerValue = headerEnumerator.Current;
                    }
                }
            }
            catch // (Exception ex)
            {
                // ignored
            }

            return headerValue;
        }

        #endregion

    }
}