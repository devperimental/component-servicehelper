using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.Queues.Behaviours;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.Settings.Shared.Behaviours;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Settings.Shared.Config;

namespace PlatformX.ServiceLayer.NTesting
{
    public class MicroServiceQueueHelperTest
    {
        private EndpointHelperConfiguration _endpointHelperConfiguration;
        private RequestContext _requestContext;
        private GenericRequest _genericRequest;
        private string _tenantId = "tenantId";
        private string _environment = "dev";

        [SetUp]
        public void Init()
        {
            _endpointHelperConfiguration = new EndpointHelperConfiguration
            {
                Prefix = "dz",
                Environment = "dev",
                RoleKey = "mgmt",
                Location = "syd",
                Region = "au"
            };

            _requestContext = new RequestContext
            {
                PortalName = "Portal",
                IpAddress = TestHelper.Default_IpAddress,
                CorrelationId = TestHelper.Default_CorrelationId,
                SessionId = Guid.NewGuid().ToString(),
                IdentityId = Guid.NewGuid().ToString(),
                OrganisationGlobalId = Guid.NewGuid().ToString(),
                UserGlobalId = Guid.NewGuid().ToString(),
                SystemApiRoleType = SystemApiRoleType.Management,
                ClientAppEnvironment = "MGMT"
            };

            _genericRequest = new GenericRequest
            {
                CorrelationId = TestHelper.Default_CorrelationId
            };
        }

        [Test]
        public async Task TestServiceQueue()
        {
            var hashGenerationHelper = new HashGenerationHelper();
            var queueClient = new Mock<IQueueXClient>();
            var traceLogger = new Mock<ILogger<MicroServiceQueueHelperTest>>();
            var protectedConfiguration = new Mock<IProtectedConfiguration>();

            var serviceMetaData = CreateServiceMetaData();

            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);

            var helper = new MicroServiceQueueHelper<MicroServiceQueueHelperTest>(protectedConfiguration.Object, 
                hashGenerationHelper,
                queueClient.Object,
                traceLogger.Object);

            var response = await helper.SubmitQueueMessage<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "generic-request-ut");

            Assert.IsTrue((response.Success));
        }

        [Test]
        public async Task TestServiceManagementQueue()
        {
            var hashGenerationHelper = new HashGenerationHelper();
            var queueClient = new Mock<IQueueXClient>();
            var traceLogger = new Mock<ILogger<MicroServiceQueueHelperTest>>();
            var protectedConfiguration = new Mock<IProtectedConfiguration>();

            var serviceMetaData = CreateServiceMetaData();

            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);

            var helper = new MicroServiceQueueHelper<MicroServiceQueueHelperTest>(protectedConfiguration.Object,
                hashGenerationHelper,
                queueClient.Object,
                traceLogger.Object);

            var response = await helper.SubmitQueueMessage<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "generic-request-ut", "mgmt", "au", "est");

            Assert.IsTrue((response.Success));
        }

        [Test]
        public async Task TestServiceClientQueue()
        {
            var hashGenerationHelper = new HashGenerationHelper();
            var queueClient = new Mock<IQueueXClient>();
            var traceLogger = new Mock<ILogger<MicroServiceQueueHelperTest>>();
            var protectedConfiguration = new Mock<IProtectedConfiguration>();

            var serviceMetaData = CreateServiceMetaData();

            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
            protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);

            var helper = new MicroServiceQueueHelper<MicroServiceQueueHelperTest>(protectedConfiguration.Object,
                hashGenerationHelper,
                queueClient.Object,
                traceLogger.Object);

            var response = await helper.SubmitQueueMessage<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "generic-request-ut", "clnt", "au", "est");

            Assert.IsTrue((response.Success));
        }

        private ServiceMetaData CreateServiceMetaData()
        {
            var serviceMetaData = new ServiceMetaData
            {
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "SERVICE-KEY" },
                    {"ServiceSecret", "SERVICE-SECRET" }
                }
            };

            return serviceMetaData;
        }
    }
}