using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PlatformX.Common.Types.DataContract;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.ServiceLayer.Behaviours;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.Settings.Behaviours;
using System;
using System.Net.Http;
using PlatformX.Common.NTesting;

namespace PlatformX.ServiceLayer.NTesting
{
    [TestFixture]
    public class MicroServiceServerHelperTestMgmt
    {
        private ServiceMetaData _serviceMetaData;
        private BootstrapConfiguration _bootstrapConfiguration;
        private HashGenerationHelper _hashGenerationHelper;
        private RequestContext _requestContext;

        [SetUp]
        public void Setup()
        {
            _bootstrapConfiguration ??=
                TestHelper.GetConfiguration<BootstrapConfiguration>(TestContext.CurrentContext.TestDirectory, "Bootstrap");

            _serviceMetaData ??= JsonConvert.DeserializeObject<ServiceMetaData>(_bootstrapConfiguration.PlatformServiceMetaData);

            _hashGenerationHelper ??= new HashGenerationHelper();

            _requestContext ??= new RequestContext
            {
                PortalName = _bootstrapConfiguration.PortalName,
                IpAddress = TestConstants.Default_IpAddress,
                CorrelationId = TestConstants.Default_IpAddress,
                SessionId = Guid.NewGuid().ToString(),
                IdentityId = Guid.NewGuid().ToString(),
                OrganisationGlobalId = Guid.NewGuid().ToString(),
                UserGlobalId = Guid.NewGuid().ToString(),
                SystemApiRoleType = SystemApiRoleType.Management,
                ClientAppEnvironment = "MGMT"
            };
        }

        private IMicroServiceServerHelper CreateMicroServiceServerHelper()
        {
            var appSettings = new Mock<IPortalSettings>();
            
            appSettings.Setup(c => c.GetSecretString(_serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestConstants.Default_ServiceKeyValue);
            appSettings.Setup(c => c.GetSecretString(_serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestConstants.Default_ServiceSecretValue);

            appSettings.Setup(c => c.GetBool(ServiceConfigurationKeyType.CheckTimestamp.ToString())).Returns(false);
            appSettings.Setup(c => c.GetInt(ServiceConfigurationKeyType.CallExpirySeconds.ToString())).Returns(300);

            return new MicroServiceServerHelper(appSettings.Object, _hashGenerationHelper, _serviceMetaData.Keys);
        }
        [Test]
        public void TestGetRequestContextHttp()
        {
            var applicationServiceTimestamp = DateTime.UtcNow.Ticks.ToString();

            var helper = CreateMicroServiceServerHelper();
            var traceLogger = new Mock<ILogger<MicroServiceServerHelperTestMgmt>>();

            // client Settings
            var requestHash = _hashGenerationHelper.GenerateRequestInput(TestConstants.Default_ServiceKeyValue, TestConstants.Default_ServiceSecretValue, applicationServiceTimestamp, _requestContext.IpAddress, _requestContext.CorrelationId);

            var httpRequestMessage = new HttpRequestMessage();
            
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceHash, requestHash);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceKey, TestConstants.Default_ServiceKeyValue);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.RequestServiceTimestamp, applicationServiceTimestamp);

            httpRequestMessage.Headers.Add(ServiceHeaderConstants.PortalName, _requestContext.PortalName);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.IpAddress, _requestContext.IpAddress);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.CorrelationId, _requestContext.CorrelationId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.SessionId, _requestContext.SessionId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.IdentityId, _requestContext.IdentityId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.OrganisationGlobalId, _requestContext.OrganisationGlobalId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.UserGlobalId, _requestContext.UserGlobalId);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.SystemApiRoleType, _requestContext.SystemApiRoleType);
            httpRequestMessage.Headers.Add(ServiceHeaderConstants.ClientAppEnvironment, _requestContext.ClientAppEnvironment);

            var requestContextParsed = helper.GetRequestContext(httpRequestMessage, traceLogger.Object);

            Assert.IsTrue(requestContextParsed.ResponseCode == 200);
            Assert.IsTrue(requestContextParsed.PortalName == _requestContext.PortalName);
            Assert.IsTrue(requestContextParsed.CorrelationId == _requestContext.CorrelationId);
            Assert.IsTrue(requestContextParsed.IpAddress == _requestContext.IpAddress);
            Assert.IsTrue(requestContextParsed.SessionId == _requestContext.SessionId);
            Assert.IsTrue(requestContextParsed.IdentityId == _requestContext.IdentityId);
            Assert.IsTrue(requestContextParsed.OrganisationGlobalId == _requestContext.OrganisationGlobalId);
            Assert.IsTrue(requestContextParsed.UserGlobalId == _requestContext.UserGlobalId);
            Assert.IsTrue(requestContextParsed.SystemApiRoleType == _requestContext.SystemApiRoleType);
            Assert.IsTrue(requestContextParsed.ClientAppEnvironment == _requestContext.ClientAppEnvironment);
        }

        [Test]
        public void TestGetRequestContextQueue()
        {
            
            var applicationServiceTimestamp = DateTime.UtcNow.Ticks.ToString();

            var helper = CreateMicroServiceServerHelper();
            var traceLogger = new Mock<ILogger<MicroServiceServerHelperTestMgmt>>();

            var requestHash = _hashGenerationHelper.GenerateRequestInput(TestConstants.Default_ServiceKeyValue, TestConstants.Default_ServiceSecretValue, applicationServiceTimestamp, _requestContext.IpAddress, _requestContext.CorrelationId);

            var message = new Message();

            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceHash, requestHash);
            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceKey, TestConstants.Default_ServiceKeyValue);
            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceTimestamp, applicationServiceTimestamp);
            message.UserProperties.Add(ServiceHeaderConstants.PortalName, _requestContext.PortalName);
            message.UserProperties.Add(ServiceHeaderConstants.IpAddress, _requestContext.IpAddress);
            message.UserProperties.Add(ServiceHeaderConstants.SessionId, _requestContext.SessionId);
            message.UserProperties.Add(ServiceHeaderConstants.CorrelationId, _requestContext.CorrelationId);
            message.UserProperties.Add(ServiceHeaderConstants.IdentityId, _requestContext.IdentityId);
            message.UserProperties.Add(ServiceHeaderConstants.OrganisationGlobalId, _requestContext.OrganisationGlobalId);
            message.UserProperties.Add(ServiceHeaderConstants.UserGlobalId, _requestContext.UserGlobalId);
            message.UserProperties.Add(ServiceHeaderConstants.SystemApiRoleType, _requestContext.SystemApiRoleType);
            message.UserProperties.Add(ServiceHeaderConstants.ClientAppEnvironment, _requestContext.ClientAppEnvironment);

            var requestContextParsed = helper.GetRequestContext(message.UserProperties, traceLogger.Object);

            Assert.IsTrue(requestContextParsed.ResponseCode == 200);
            Assert.IsTrue(requestContextParsed.PortalName == _requestContext.PortalName);
            Assert.IsTrue(requestContextParsed.IpAddress == _requestContext.IpAddress);
            Assert.IsTrue(requestContextParsed.CorrelationId == _requestContext.CorrelationId);
            Assert.IsTrue(requestContextParsed.SessionId == _requestContext.SessionId);
            Assert.IsTrue(requestContextParsed.IdentityId == _requestContext.IdentityId);
            Assert.IsTrue(requestContextParsed.OrganisationGlobalId == _requestContext.OrganisationGlobalId);
            Assert.IsTrue(requestContextParsed.UserGlobalId == _requestContext.UserGlobalId);
            Assert.IsTrue(requestContextParsed.SystemApiRoleType == _requestContext.SystemApiRoleType);
            Assert.IsTrue(requestContextParsed.ClientAppEnvironment == _requestContext.ClientAppEnvironment);
        }

        [Test]
        public void TestGetRequestContextQueueWithIdentity()
        {
            var applicationServiceTimestamp = DateTime.UtcNow.Ticks.ToString();

            var helper = CreateMicroServiceServerHelper();
            var traceLogger = new Mock<ILogger<MicroServiceServerHelperTestMgmt>>();

            var requestHash = _hashGenerationHelper.GenerateRequestInput(TestConstants.Default_ServiceKeyValue, TestConstants.Default_ServiceSecretValue, applicationServiceTimestamp, _requestContext.IpAddress, _requestContext.CorrelationId);

            var message = new Message();

            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceHash, requestHash);
            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceKey, TestConstants.Default_ServiceKeyValue);
            message.UserProperties.Add(ServiceHeaderConstants.RequestServiceTimestamp, applicationServiceTimestamp);
            
            message.UserProperties.Add(ServiceHeaderConstants.PortalName, _requestContext.PortalName);
            message.UserProperties.Add(ServiceHeaderConstants.IpAddress, _requestContext.IpAddress);
            message.UserProperties.Add(ServiceHeaderConstants.CorrelationId, _requestContext.CorrelationId);
            message.UserProperties.Add(ServiceHeaderConstants.SessionId, _requestContext.SessionId);
            message.UserProperties.Add(ServiceHeaderConstants.IdentityId, _requestContext.IdentityId);
            message.UserProperties.Add(ServiceHeaderConstants.OrganisationGlobalId, _requestContext.OrganisationGlobalId);
            message.UserProperties.Add(ServiceHeaderConstants.UserGlobalId, _requestContext.UserGlobalId);
            message.UserProperties.Add(ServiceHeaderConstants.SystemApiRoleType, _requestContext.SystemApiRoleType);
            message.UserProperties.Add(ServiceHeaderConstants.ClientAppEnvironment, _requestContext.ClientAppEnvironment);

            var requestContextParsed = helper.GetRequestContext(message.UserProperties, traceLogger.Object);

            Assert.IsTrue(requestContextParsed.ResponseCode == 200);
            
            Assert.IsTrue(requestContextParsed.PortalName == _requestContext.PortalName);
            Assert.IsTrue(requestContextParsed.CorrelationId == _requestContext.CorrelationId);
            Assert.IsTrue(requestContextParsed.IpAddress == _requestContext.IpAddress);
            Assert.IsTrue(requestContextParsed.SessionId == _requestContext.SessionId);
            Assert.IsTrue(requestContextParsed.IdentityId == _requestContext.IdentityId); 
            Assert.IsTrue(requestContextParsed.OrganisationGlobalId == _requestContext.OrganisationGlobalId);
            Assert.IsTrue(requestContextParsed.UserGlobalId == _requestContext.UserGlobalId);
            Assert.IsTrue(requestContextParsed.SystemApiRoleType == _requestContext.SystemApiRoleType);
            Assert.IsTrue(requestContextParsed.ClientAppEnvironment == _requestContext.ClientAppEnvironment);
        }
    }
}