using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Queues.ServiceBus;
using PlatformX.Settings.Helper;
using PlatformX.Settings.Shared.Config;

namespace PlatformX.Queues.NTesting
{
    [TestFixture]
    public class ServiceBusQueueClientTest
    {
        private EndpointHelperConfiguration _endpointHelperConfiguration;
        private RequestContext? _requestContext;
        private GenericRequest? _genericRequest;
        private string _tenantId = "tenantId";

        [SetUp]
        public void Init()
        {
            if (_requestContext == null)
            {
                _requestContext = new RequestContext
                {
                    PortalName = "PortalName",
                    IpAddress = TestHelper.Default_IpAddress,
                    CorrelationId = TestHelper.Default_IpAddress,
                    SessionId = Guid.NewGuid().ToString(),
                    IdentityId = Guid.NewGuid().ToString()
                };
            }

            _genericRequest = new GenericRequest
            {
                CorrelationId = TestHelper.Default_CorrelationId
            };

            _endpointHelperConfiguration = new EndpointHelperConfiguration
            {
                Prefix = "dz",
                Environment = "dev",
                RoleKey = "mgmt",
                Location = "syd",
                Region = "au"
            };
        }

        private async Task TestSendMessageInternal(string mode = "")
        {
            var endpointHelper = new EndpointHelper(_endpointHelperConfiguration);
            var applicationServiceTimestamp = DateTime.UtcNow.Ticks.ToString();

            var traceLogger = new Mock<ILogger<ServiceBusQueueClientTest>>();
            var serviceBusClient = new ServiceBusQueueClient<ServiceBusQueueClientTest>(traceLogger.Object, endpointHelper, _tenantId);
            var messageId = Guid.NewGuid().ToString();

            var hashGenerationHelper = new HashGenerationHelper();
            var requestHash = hashGenerationHelper.GenerateRequestInput(TestHelper.Default_ServiceKeyValue, TestHelper.Default_ServiceSecretValue, applicationServiceTimestamp, _requestContext.IpAddress, _requestContext.CorrelationId);

            var headers = new Dictionary<string, string>
            {
                {ServiceHeaderConstants.RequestServiceHash, requestHash},
                {ServiceHeaderConstants.PortalName, _requestContext.PortalName},
                {ServiceHeaderConstants.RequestServiceKey, TestHelper.Default_ServiceKeyValue},
                {ServiceHeaderConstants.RequestServiceTimestamp, applicationServiceTimestamp},
                {ServiceHeaderConstants.IpAddress, _requestContext.IpAddress},
                {ServiceHeaderConstants.CorrelationId, _requestContext.CorrelationId},
                {ServiceHeaderConstants.SessionId, _requestContext.SessionId},
                {ServiceHeaderConstants.IdentityId, _requestContext.IdentityId}
            };

            var data = JsonConvert.SerializeObject(_genericRequest);

            switch (mode)
            {
                case "C":
                    await serviceBusClient.SendClientMessage(data, headers, messageId, "log-entry", "au", "est");
                    break;
                case "M":
                    await serviceBusClient.SendManagementMessage(data, headers, messageId, "log-entry", "au", "est");
                    break;
                default:
                    await serviceBusClient.SendMessage(data, headers, messageId, "mgmt", "au", "est", "log-entry");
                    break;
            }
        }

        [Test]

        public async Task TestSendMessage()
        {
            await TestSendMessageInternal();
            Assert.IsTrue(true);
        }

        [Test]

        public async Task TestSendManagementMessage()
        {
            await TestSendMessageInternal("M");
            Assert.IsTrue(true);
        }

        [Test]

        public async Task TestSendClientMessage()
        {
            await TestSendMessageInternal("C");
            Assert.IsTrue(true);
        }
    }
}