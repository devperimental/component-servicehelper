using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PlatformX.Common.Types.DataContract;
using PlatformX.FunctionLayer.Helper;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.Settings.Behaviours;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using PlatformX.Common.NTesting;

namespace PlatformX.FunctionLayer.NTesting
{
    public class FuntionHelperTest
    {
        private ServiceMetaData _serviceMetaData;
        private BootstrapConfiguration _bootstrapConfiguration;

        private string _hashOutcome;
        private string _requestServiceTimestamp;
        private string _ipAddress;
        private string _correlationId;
        private string _portalName;
        private string _sessionId;
        private string _identityId;
        private string _userGlobalId;
        private string _organisationGlobalId;
        private string _systemApiRoleType;

        [SetUp]
        public void Setup()
        {
            if (_bootstrapConfiguration == null)
            {
                _bootstrapConfiguration = TestHelper.GetConfiguration<BootstrapConfiguration>(TestContext.CurrentContext.TestDirectory, "Bootstrap");
            }

            if (_serviceMetaData == null)
            {
                _serviceMetaData = JsonConvert.DeserializeObject<ServiceMetaData>(_bootstrapConfiguration.PlatformServiceMetaData);
            }

            _hashOutcome = "t5lcSDzNd05GCt3CKV3qXh5OUOEnt/VQmEItNVm7sHZnTHrY0SaGJQdDuI094c8sI4+pHbP+1EECVZZm1iP4RQ==";
            
            _requestServiceTimestamp = "637209686814252385";
            _ipAddress = TestConstants.Default_IpAddress;
            _correlationId = TestConstants.Default_CorrelationId;
            _portalName = _bootstrapConfiguration.PortalName;
            _sessionId = Guid.NewGuid().ToString();
            _identityId = Guid.NewGuid().ToString();
            _userGlobalId = Guid.NewGuid().ToString();
            _organisationGlobalId = Guid.NewGuid().ToString();
            _systemApiRoleType = SystemApiRoleType.Management;
        }

        [Test]
        public async Task TestExecuteHTTPCall()
        {
            var portalSettings = new Mock<IPortalSettings>();
            var hashGenerationHelper = new HashGenerationHelper();
            var traceLogger = new Mock<ILogger<FuntionHelperTest>>();

            portalSettings.Setup(c => c.GetSecretString(_serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestConstants.Default_ServiceKeyValue);
            portalSettings.Setup(c => c.GetSecretString(_serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestConstants.Default_ServiceSecretValue);

            portalSettings.Setup(c => c.GetBool(ServiceConfigurationKeyType.CheckTimestamp.ToString())).Returns(false);
            portalSettings.Setup(c => c.GetInt(ServiceConfigurationKeyType.CallExpirySeconds.ToString())).Returns(300);

            var microServiceServerHelper = new MicroServiceServerHelper(portalSettings.Object, hashGenerationHelper, _serviceMetaData.Keys);

            var functionHelper = new FunctionHelper(microServiceServerHelper);

            var req = CreateRequest();

            var result = await functionHelper.ExecuteHttpCall(async (requestContext, requestJSON) =>
            {
                var response = new PingResponse
                {
                    Success = true,
                    Message = "PingFH complete"
                };

                Assert.IsTrue(requestContext.IdentityId == _identityId);
                return await Task.FromResult(new OkObjectResult(true));
            }, req, "TestController", "PingFH", traceLogger.Object);
        }

        
        private HttpRequestMessage CreateRequest()
        {
            var httpRequestMessage = new HttpRequestMessage();
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceHash, _hashOutcome);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.PortalName, _portalName);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceKey, TestConstants.Default_ServiceKeyValue);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceTimestamp, _requestServiceTimestamp);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.IpAddress, _ipAddress);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.CorrelationId, _correlationId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.SessionId, _sessionId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.IdentityId, _identityId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.UserGlobalId, _userGlobalId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.OrganisationGlobalId, _organisationGlobalId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.SystemApiRoleType, _systemApiRoleType);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.ClientAppEnvironment, "MGMT");

            return httpRequestMessage;
        }
    }
}