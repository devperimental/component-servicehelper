using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Settings.Helper;
using PlatformX.Http.Helper;
using PlatformX.Settings.Types;
using PlatformX.Secrets.Azure;
using PlatformX.Settings.Shared.Config;

namespace PlatformX.ServiceLayer.NTesting
{
    public class MicroServiceRoleClientHelperTest
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

        //public MicroServiceRoleClientHelper<MicroServiceRoleClientHelperTest> InitServiceClientHttp(ServiceMetaData serviceMetaData, string operationName, bool authorized = false)
        //{
            
        //    var httpRequestHelper = new HttpRequestHelper();
        //    var hashGenerationHelper = new HashGenerationHelper();
        //    var traceLogger = new Mock<ILogger<MicroServiceRoleClientHelperTest>>();
        //    var endpointHelper = new EndpointHelper(_bootstrapConfiguration);
        //    var secretLoader = new KeyVaultSecretLoader<MicroServiceRoleClientHelperTest>(_bootstrapConfiguration, traceLogger.Object, endpointHelper);
        //    var protectedRoleConfiguration = new ProtectedRoleConfiguration(_bootstrapConfiguration, secretLoader);

        //    var uri = string.Empty;

        //    if (authorized)
        //    {
        //        uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName, TestHelper.Default_ServiceSecretValue);
        //    }
        //    else
        //    {
        //        uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName);
        //    }

        //    var helper = new MicroServiceRoleClientHelper<MicroServiceRoleClientHelperTest>(protectedRoleConfiguration,
        //        httpRequestHelper,
        //        hashGenerationHelper,
        //        traceLogger.Object);

        //    return helper;
        //}

        //[Test]
        //public void TestServiceClientHttp()
        //{
        //    var operationName = "PingHTTP";
        //    var serviceMetaData = CreateServiceMetaData();
        //    var helper = InitServiceClientHttp(serviceMetaData, operationName);
        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, operationName, "dev", "1000", "au", "est", "C");

        //    Assert.IsTrue(response.Success);
        //}

        public ServiceMetaData CreateServiceMetaData()
        {
            var serviceMetaData = new ServiceMetaData
            {
                Name = "Identity",
                HostEndpointType = ServiceEndpointType.AZF,
                HostRoleType = SystemApiRoleType.Client,
                RoleTypeSettings = new Dictionary<string, RoleTypeSetting>
                {
                    { 
                        SystemApiRoleType.Client, 
                        new RoleTypeSetting
                        {
                             RegionKey = "au",
                             LocationKey = "est",
                             RoleKey = SystemRoleKey.Client
                        }
                    },
                    {
                        SystemApiRoleType.Management,
                        new RoleTypeSetting
                        {
                             RegionKey = "au",
                             LocationKey = "est",
                             RoleKey = SystemRoleKey.Management
                        }
                    }
                },
                Endpoints = new List<ServiceEndpoint>
                {
                    {
                        new ServiceEndpoint
                        {
                             IsPrimary = true,
                             Uri = "http://localhost:1111/Platform/{0}",
                             Type = ServiceEndpointType.AZF,
                             AccessKey = "IdentityService-AZF-KEY",
                             FulfillmentRoleType = SystemApiRoleType.Client,
                             Operations = new List<ServiceOperation>
                             {
                                  new ServiceOperation
                                  {
                                        Name = "PingHTTP",
                                        Protocol = ServiceEndpointProtocol.HTTP,
                                        Parameters = new Dictionary<string, string>()
                                        {
                                            { "METHOD","Post" },
                                            { "AUTHORIZE","true" }
                                        }
                                  }
                             }
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "IDENTITY-SERVICE-KEY" },
                    {"ServiceSecret", "IDENTITY-SERVICE-SECRET" }
                }
            };

            return serviceMetaData;
        }

        
    }
}