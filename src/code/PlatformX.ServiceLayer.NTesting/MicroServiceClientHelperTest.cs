using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;
using PlatformX.Http.Behaviours;
using PlatformX.Messaging.Helper;
using PlatformX.Messaging.Types;
using PlatformX.Messaging.Types.Constants;
using PlatformX.Messaging.Types.EnumTypes;
using PlatformX.Queues.Behaviours;
using PlatformX.ServiceLayer.Helper;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using PlatformX.Settings.Helper;
using PlatformX.Settings.Shared.Behaviours;
using PlatformX.Settings.Shared.Config;
using PlatformX.Storage.Azure;
using PlatformX.Storage.Behaviours;
using PlatformX.Storage.StoreClient;

namespace PlatformX.ServiceLayer.NTesting
{
    public class MicroServiceClientHelperTest
    {
        private EndpointHelperConfiguration _endpointHelperConfiguration;
        private RequestContext _requestContext;
        private GenericRequest _genericRequest;
        private string _tenantId = "tenantId";
        private string _environment = "dev";
        
        //[SetUp]
        //public void Init()
        //{
        //    _endpointHelperConfiguration = new EndpointHelperConfiguration
        //    {
        //        Prefix = "dz",
        //        Environment = "dev",
        //        RoleKey = "mgmt",
        //        Location = "syd",
        //        Region = "au"
        //    };

        //    _requestContext = new RequestContext
        //    {
        //        PortalName = "Portal",
        //        IpAddress = TestHelper.Default_IpAddress,
        //        CorrelationId = TestHelper.Default_CorrelationId,
        //        SessionId = Guid.NewGuid().ToString(),
        //        IdentityId = Guid.NewGuid().ToString(),
        //        OrganisationGlobalId = Guid.NewGuid().ToString(),
        //        UserGlobalId = Guid.NewGuid().ToString(),
        //        SystemApiRoleType = SystemApiRoleType.Management,
        //        ClientAppEnvironment = "MGMT"
        //    };

        //    _genericRequest = new GenericRequest
        //    {
        //        CorrelationId = TestHelper.Default_CorrelationId
        //    };
        //}

        //public MicroServiceClientHelper<MicroServiceClientHelperTest> InitServiceClientHttp(ServiceMetaData serviceMetaData, string operationName, bool authorized = false)
        //{
        //    var httpRequestHelper = new Mock<IHttpRequestHelper>();
        //    var hashGenerationHelper = new HashGenerationHelper();
        //    var queueClient = new Mock<IQueueXClient>();
        //    var traceLogger = new Mock<ILogger<MicroServiceClientHelperTest>>();
        //    var fileStore = new Mock<IFileStore>();

        //    var protectedConfiguration = new Mock<IProtectedConfiguration>();

        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);
        //    protectedConfiguration.Setup(c => c.GetSecretString("PlatformService-AZF-KEY")).Returns(TestHelper.Default_ServiceSecretValue);
            
        //    var uri = string.Empty;

        //    if (authorized)
        //    {
        //        uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName, TestHelper.Default_ServiceSecretValue);
        //    }
        //    else
        //    {
        //        uri = string.Format(serviceMetaData.Endpoints[0].Uri, operationName);
        //    }

        //    httpRequestHelper.Setup(c => c.SubmitRequest<GenericResponse>(uri, It.IsAny<HttpMethod>(), It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).Returns(Task.FromResult(new GenericResponse { Success = true }));

        //    var helper = new MicroServiceClientHelper<MicroServiceClientHelperTest>(protectedConfiguration.Object,
        //        httpRequestHelper.Object,
        //        queueClient.Object,
        //        hashGenerationHelper,
        //        traceLogger.Object,
        //        fileStore.Object);

        //    return helper;
        //}

        //[Test]
        //public void TestServiceClientHttp()
        //{
        //    var operationName = "PingHTTP";
        //    var serviceMetaData = CreateServiceMetaData();
        //    var helper = InitServiceClientHttp(serviceMetaData, operationName);
        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, operationName);

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void TestServiceClientHttpAuthorized()
        //{
        //    var operationName = "PingHTTP";
        //    var serviceMetaData = CreateServiceMetaDataAlt();

        //    var helper = InitServiceClientHttp(serviceMetaData, operationName, true);
        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, operationName);

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void TestServiceClientHttpWithCache()
        //{
        //    var operationName = "PingData";
        //    var serviceMetaData = CreateServiceMetaData();
        //    var helper = InitServiceClientHttp(serviceMetaData, operationName);
        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, operationName);

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void TestServiceClientQueue()
        //{
        //    var httpRequestHelper = new Mock<IHttpRequestHelper>();
        //    var hashGenerationHelper = new HashGenerationHelper();
        //    var traceLogger = new Mock<ILogger<MicroServiceClientHelperTest>>();
        //    var queueClient = new Mock<IQueueXClient>();
        //    var fileStore = new Mock<IFileStore>();
            
        //    var serviceMetaData = CreateServiceMetaData();

        //    var protectedConfiguration = new Mock<IProtectedConfiguration>();

        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);
        //    //protectedConfiguration.SetupGet(c => c.BootstrapConfiguration).Returns(new BootstrapConfiguration { Environment = "local" });

        //    var helper = new MicroServiceClientHelper<MicroServiceClientHelperTest>(protectedConfiguration.Object, httpRequestHelper.Object, queueClient.Object, hashGenerationHelper, traceLogger.Object, fileStore.Object);

        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "PingQueue");

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void TestLoadMetaDataFromFile()
        //{
        //    var httpRequestHelper = new Mock<IHttpRequestHelper>();
        //    var hashGenerationHelper = new HashGenerationHelper();
        //    var traceLogger = new Mock<ILogger<MicroServiceClientHelperTest>>();
        //    var queueClient = new Mock<IQueueXClient>();
        //    var fileStore = new Mock<IFileStore>();
            
        //    var serviceMetaData = CreateServiceMetaData();

        //    var protectedConfiguration = new Mock<IProtectedConfiguration>();

        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);
        //    //protectedConfiguration.SetupGet(c => c.BootstrapConfiguration).Returns(new BootstrapConfiguration { Environment= "local"});

        //    var helper = new MicroServiceClientHelper<MicroServiceClientHelperTest>(protectedConfiguration.Object, httpRequestHelper.Object, queueClient.Object, hashGenerationHelper, traceLogger.Object, fileStore.Object);

        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "PingQueue");

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void SerializeServiceMetaData()
        //{
        //    var serviceMetaData = CreateServiceMetaData();

        //    var serializedServiceMetaData = JsonConvert.SerializeObject(serviceMetaData);

        //    Assert.IsNotNull(serializedServiceMetaData);
        //}

        //public ServiceMetaData CreateServiceMetaData()
        //{
        //    var serviceMetaData = new ServiceMetaData
        //    {
        //        Name = "TestService",
        //        HostEndpointType = ServiceEndpointType.AZF,
        //        HostRoleType = SystemApiRoleType.Client,
        //        RoleTypeSettings = new Dictionary<string, RoleTypeSetting>
        //        {
        //            { 
        //                SystemApiRoleType.Client, 
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Client
        //                }
        //            },
        //            {
        //                SystemApiRoleType.Management,
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Management
        //                }
        //            }
        //        },
        //        Endpoints = new List<ServiceEndpoint>
        //        {
        //            {
        //                new ServiceEndpoint
        //                {
        //                     IsPrimary = true,
        //                     Uri = "http://localhost:1111/Platform/{0}",
        //                     Type = ServiceEndpointType.AZF,
        //                     AccessKey = "PlatformService-AZF-KEY",
        //                     FulfillmentRoleType = SystemApiRoleType.Client,
        //                     Operations = new List<ServiceOperation>
        //                     {
        //                          new ServiceOperation
        //                          {
        //                                Name = "PingHTTP",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          },
        //                          new ServiceOperation
        //                          {
        //                                Name = "PingQueue",
        //                                Protocol = ServiceEndpointProtocol.QUEUE,
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "QUEUENAME","TestQueue" } // MessageId into telemetry engine
        //                                }
        //                          },
        //                          new ServiceOperation
        //                          {
        //                                Name = "PingData",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Cache = new OperationCacheSettings
        //                                {
        //                                    Enabled = true,
        //                                    CacheType = OperationCacheType.File,
        //                                    FileKey = "Data/{MethodName}.json"
        //                                },
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          },
        //                     }
        //                }
        //            }
        //        },
        //        Keys = new Dictionary<string, string>
        //        {
        //            {"ServiceKey", "SERVICE-KEY" },
        //            {"ServiceSecret", "SERVICE-SECRET" }
        //        }
        //    };

        //    return serviceMetaData;
        //}

        //public ServiceMetaData CreateServiceMetaDataAlt()
        //{
        //    var serviceMetaData = new ServiceMetaData
        //    {
        //        Name = "TestService",
        //        HostEndpointType = ServiceEndpointType.AZF,
        //        HostRoleType = SystemApiRoleType.Client,
        //        RoleTypeSettings = new Dictionary<string, RoleTypeSetting>
        //        {
        //            {
        //                SystemApiRoleType.Client,
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Client
        //                }
        //            },
        //            {
        //                SystemApiRoleType.Management,
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Management
        //                }
        //            }
        //        },
        //        Endpoints = new List<ServiceEndpoint>
        //        {
        //            {
        //                new ServiceEndpoint
        //                {
        //                     IsPrimary = true,
        //                     Uri = "http://localhost:1111/Platform/{0}?code={1}",
        //                     Type = ServiceEndpointType.AZF,
        //                     AccessKey = "PlatformService-AZF-KEY",
        //                     FulfillmentRoleType = SystemApiRoleType.Client,
        //                     Operations = new List<ServiceOperation>
        //                     {
        //                          new ServiceOperation
        //                          {
        //                                Name = "PingHTTP",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","true" }
        //                                }
        //                          }
        //                     }
        //                }
        //            }
        //        },
        //        Keys = new Dictionary<string, string>
        //        {
        //            {"ServiceKey", "SERVICE-KEY" },
        //            {"ServiceSecret", "SERVICE-SECRET" }
        //        }
        //    };

        //    return serviceMetaData;
        //}

        //public MicroServiceClientHelper<MicroServiceClientHelperTest> ConfigureServiceClientForFileStore(ServiceMetaData serviceMetaData)
        //{
        //    var httpRequestHelper = new Mock<IHttpRequestHelper>();
            
        //    httpRequestHelper.Setup(c => c.SubmitRequest<GenericResponse>(It.IsAny<string>(), HttpMethod.Post, It.IsAny<string>(), It.IsAny<Dictionary<string, string>>())).Returns(Task.FromResult(new GenericResponse { Message = "Hello", Success = true }));

        //    var hashGenerationHelper = new HashGenerationHelper();
        //    var traceLogger = new Mock<ILogger<MicroServiceClientHelperTest>>();
        //    var queueClient = new Mock<IQueueXClient>();
        //    var storageProvider = new AzureStorage<MicroServiceClientHelperTest>(_environment, traceLogger.Object);
        //    var endpointHelper = new EndpointHelper(_endpointHelperConfiguration);
        //    var fileStore = new FileStore(storageProvider, endpointHelper, _tenantId);

        //    var protectedConfiguration = new Mock<IProtectedConfiguration>();

        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceKey.ToString()])).Returns(TestHelper.Default_ServiceKeyValue);
        //    protectedConfiguration.Setup(c => c.GetSecretString(serviceMetaData.Keys[ServiceConfigurationKeyType.ServiceSecret.ToString()])).Returns(TestHelper.Default_ServiceSecretValue);
        //    //protectedConfiguration.SetupGet(c => c.BootstrapConfiguration).Returns(_bootstrapConfiguration);

        //    var helper = new MicroServiceClientHelper<MicroServiceClientHelperTest>(protectedConfiguration.Object, httpRequestHelper.Object, queueClient.Object, hashGenerationHelper, traceLogger.Object, fileStore);

        //    return helper;
        //}

        //[Test]
        //public void TestServiceClientWithFileStore()
        //{
        //    var serviceMetaData = CreateServiceMetaDataWithStore();
        //    var helper = ConfigureServiceClientForFileStore(serviceMetaData);

        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "GetServiceLayerConfiguration");

        //    Assert.IsTrue(response.Success);
        //}

        //[Test]
        //public void TestServiceClientWithFileStoreAlt()
        //{
        //    var serviceMetaData = CreateServiceMetaDataWithStore();
        //    var helper = ConfigureServiceClientForFileStore(serviceMetaData);

        //    var response = helper.SubmitRequest<GenericRequest, GenericResponse>(_genericRequest, _requestContext, serviceMetaData, "GetServiceLayerConfigurationAlt");

        //    Assert.IsTrue(response.Success);
        //}

        //public ServiceMetaData CreateServiceMetaDataWithStore()
        //{
        //    var serviceMetaData = new ServiceMetaData
        //    {
        //        Name = "Platform",
        //        HostEndpointType = ServiceEndpointType.AZF,
        //        HostRoleType = SystemApiRoleType.Client,
        //        RoleTypeSettings = new Dictionary<string, RoleTypeSetting>
        //        {
        //            {
        //                SystemApiRoleType.Client,
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Client
        //                }
        //            },
        //            {
        //                SystemApiRoleType.Management,
        //                new RoleTypeSetting
        //                {
        //                     RegionKey = "au",
        //                     LocationKey = "est",
        //                     RoleKey = SystemRoleKey.Management
        //                }
        //            }
        //        },
        //        Endpoints = new List<ServiceEndpoint>
        //        {
        //            {
        //                new ServiceEndpoint
        //                {
        //                     IsPrimary = true,
        //                     Uri = "http://localhost:5805/api/{0}",
        //                     Type = ServiceEndpointType.AZF,
        //                     AccessKey = "PlatformService-AZF-KEY",
        //                     FulfillmentRoleType = SystemApiRoleType.Client,
        //                     Operations = new List<ServiceOperation>
        //                     {
        //                          new ServiceOperation
        //                          {
        //                                Name = "Ping",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          },
        //                          new ServiceOperation
        //                          {
        //                                Name = "GetAppLayerConfiguration",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Cache = new OperationCacheSettings
        //                                {
        //                                    Enabled = true,
        //                                    CacheType = OperationCacheType.File,
        //                                    FileKey = "Data/{MethodName}.txt"
        //                                },
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          },
        //                          new ServiceOperation
        //                          {
        //                                Name = "GetServiceLayerConfiguration",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Cache = new OperationCacheSettings
        //                                {
        //                                    Enabled = true,
        //                                    CacheType = OperationCacheType.File,
        //                                    FileKey = "Data/{MethodName}.txt"
        //                                },
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          },
        //                          new ServiceOperation
        //                          {
        //                                Name = "GetServiceLayerConfigurationAlt",
        //                                Protocol = ServiceEndpointProtocol.HTTP,
        //                                Cache = new OperationCacheSettings
        //                                {
        //                                    Enabled = true,
        //                                    CacheType = OperationCacheType.File,
        //                                    FileKey = "Data/{MethodName}/{Id}.txt",
        //                                    PerRequest = true,
        //                                    PerRequestIdentifier = "CorrelationId"
        //                                },
        //                                Parameters = new Dictionary<string, string>()
        //                                {
        //                                    { "METHOD","Post" },
        //                                    { "AUTHORIZE","false" }
        //                                }
        //                          }
        //                     }
        //                }
        //            }
        //        },
        //        Keys = new Dictionary<string, string>
        //        {
        //            {"ServiceKey", "SERVICE-KEY" },
        //            {"ServiceSecret", "SERVICE-SECRET" }
        //        }
        //    };

        //    return serviceMetaData;
        //}

        //[Test]
        //public void SerializePlatformServiceMetaData()
        //{
        //    var serviceMetaData = CreateServiceMetaDataWithStore();

        //    var serializedServiceMetaData = JsonConvert.SerializeObject(serviceMetaData);

        //    Assert.IsNotNull(serializedServiceMetaData);
        //}
    }
}