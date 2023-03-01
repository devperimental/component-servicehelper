using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PlatformX.Common.Types.DataContract;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using System;
using System.Collections.Generic;
using PlatformX.Common.NTesting;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Settings.Helper;
using PlatformX.Http.Helper;
using PlatformX.Logging.Behaviours;
using PlatformX.Settings.Types;
using PlatformX.Secrets.Azure;

namespace PlatformX.ServiceLayer.NTesting
{
    public class MicroServiceRoleClientHelperTest
    {
        //private ServiceMetaData _serviceMetaData;
        private BootstrapConfiguration _bootstrapConfiguration;
        private RequestContext _requestContext;
        private GenericRequest _genericRequest;

        [SetUp]
        public void Init()
        {
            if (_bootstrapConfiguration == null)
            {
                _bootstrapConfiguration = TestHelper.GetConfiguration<BootstrapConfiguration>(TestContext.CurrentContext.TestDirectory, "Bootstrap");
            }

            _requestContext = new RequestContext
            {
                PortalName = _bootstrapConfiguration.PortalName,
                IpAddress = TestConstants.Default_IpAddress,
                CorrelationId = TestConstants.Default_CorrelationId,
                SessionId = Guid.NewGuid().ToString(),
                IdentityId = Guid.NewGuid().ToString(),
                OrganisationGlobalId = Guid.NewGuid().ToString(),
                UserGlobalId = Guid.NewGuid().ToString(),
                SystemApiRoleType = SystemApiRoleType.Management,
                ClientAppEnvironment = "MGMT"
            };

            _genericRequest = new GenericRequest
            {
                CorrelationId = TestConstants.Default_CorrelationId
            };
        }

        public MicroServiceRoleClientHelper<MicroServiceRoleClientHelperTest> InitServiceClientHttp(ServiceMetaData serviceMetaData, string operationName, bool authorized = false)
        {
            var appLogger = new Mock<IAppLogger>();
            var httpRequestHelper = new HttpRequestHelper(appLogger.Object);
            var hashGenerationHelper = new HashGenerationHelper();
            var traceLogger = new Mock<ILogger<MicroServiceRoleClientHelperTest>>();
            var endpointHelper = new EndpointHelper(_bootstrapConfiguration);
            var secretLoader = new KeyVaultSecretLoader<MicroServiceRoleClientHelperTest>(_bootstrapConfiguration, traceLogger.Object, endpointHelper);
            var protectedRoleConfiguration = new ProtectedRoleConfiguration(_bootstrapConfiguration, secretLoader);

            var uri = string.Empty;

            if (authorized)
            {
                uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName, TestConstants.Default_ServiceSecretValue);
            }
            else
            {
                uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName);
            }

            var helper = new MicroServiceRoleClientHelper<MicroServiceRoleClientHelperTest>(protectedRoleConfiguration,
                httpRequestHelper,
                hashGenerationHelper,
                traceLogger.Object);

            return helper;
        }

        [Test]
        public void TestServiceClientHttp()
        {
            var operationName = "PingHTTP";
            var serviceMetaData = CreateServiceMetaData();
            var helper = InitServiceClientHttp(serviceMetaData, operationName);
            var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, operationName, "dev", "1000", "au", "est", "C");

            Assert.IsTrue(response.Success);
        }

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