using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PlatformX.Common.Types.DataContract;
using PlatformX.ServiceLayer.Types;
using PlatformX.ServiceLayer.Types.Constants;
using PlatformX.Storage.Azure;
using PlatformX.Storage.Behaviours;
using PlatformX.Storage.StoreClient;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using PlatformX.Settings.Helper;

namespace PlatformX.Storage.NTesting
{
    [TestFixture]
    public class FileStoreTests
    {
        private BootstrapConfiguration _bootstrapConfiguration;
        private IFileStore _fileStore;

        [SetUp]
        public void Setup()
        {
            _bootstrapConfiguration ??=
                TestHelper.GetConfiguration<BootstrapConfiguration>(TestContext.CurrentContext.TestDirectory, "Bootstrap");

            if (_fileStore != null) return;
            var traceLogger = new Mock<ILogger<FileStoreTests>>();
            var storage = new AzureStorage<FileStoreTests>(_bootstrapConfiguration, traceLogger.Object);
            var endpointHelper = new EndpointHelper(_bootstrapConfiguration);
            _fileStore = new FileStore(storage, _bootstrapConfiguration, endpointHelper);
        }

        [Test]
        public void TestSaveAndLoad()
        {
            var filePath = "configuration/servicemetadata-testing.json";
            var serviceName = "Platform";
            
            var roleKey = "mgmt";
            var regionKey = "au";
            var locationKey = "est";

            var metaData = GetServiceMetaData("local");

            _fileStore.SaveFile(metaData, serviceName, roleKey, regionKey, locationKey, filePath, "text/json");

            var data = _fileStore.LoadFile<Dictionary<string, ServiceMetaData>>(serviceName, roleKey, regionKey, locationKey, filePath);

            Assert.IsNotNull(data);
        }

        [Test]
        public void TestLoadClient()
        {
            var filePath = "mail-template/metadata.json";
            var container = "Messaging";
            
            var data = _fileStore.LoadClientFile<string>(container, filePath, "au", "est");

            Assert.IsNotNull(data);
        }

        [Test]
        public void TestSaveAndLoadClient()
        {
            var filePath = "configuration/servicemetadata-testing.json";
            var container = "Platform";
            var metaData = GetServiceMetaData("local");

            _fileStore.SaveClientFile(metaData, container, filePath, "text/json", "au", "est");

            var data = _fileStore.LoadClientFile<Dictionary<string, ServiceMetaData>>(container, filePath, "au", "est");

            Assert.IsNotNull(data);
        }

        private Dictionary<string, ServiceMetaData> GetServiceMetaData(string env)
        {
            var dictionary = new Dictionary<string, ServiceMetaData>();

            var platformMetaData = new ServiceMetaData
            {
                Name = "Platform",
                Endpoints = new List<ServiceEndpoint>
                {
                    new ServiceEndpoint
                    {
                        IsPrimary = true,
                        Uri = "http://localhost:5805/api/{0}",
                        Type = ServiceEndpointType.AZF,
                        Operations = new List<ServiceOperation>
                        {
                            GetHttpServiceOperation("Ping"),
                            GetHttpServiceOperation("GetAppLayerConfiguration", true),
                            GetHttpServiceOperation("GetServiceLayerConfiguration", true)
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "PLATFORM-SERVICE-KEY" },
                    {"ServiceSecret", "PLATFORM-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Platform", platformMetaData);

            var loggingMetaData = new ServiceMetaData
            {
                Name = "Logging",
                Endpoints = new List<ServiceEndpoint>
                {
                    new ServiceEndpoint
                    {
                        IsPrimary = true,
                        Uri = "http://localhost:5815/api/{0}",
                        Type = ServiceEndpointType.AZF,
                        Operations = new List<ServiceOperation>
                        {
                            GetHttpServiceOperation("Ping"),
                            GetQueueServiceOperation("SaveLogEntry", "log-entry", env),
                            GetHttpServiceOperation("Search")
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "LOGGING-SERVICE-KEY" },
                    {"ServiceSecret", "LOGGING-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Logging", loggingMetaData);

            var messagingConfiguration = new ServiceMetaData
            {
                Name = "Messaging",
                Endpoints = new List<ServiceEndpoint>
                {
                    {
                        new ServiceEndpoint
                        {
                             IsPrimary = true,
                             Uri = "http://localhost:5825/api/{0}",
                             Type = ServiceEndpointType.AZF,
                             Operations = new List<ServiceOperation>
                             {
                                 GetHttpServiceOperation("Ping"),
                                 GetQueueServiceOperation("SendEmail", "email-container", env),
                                 GetQueueServiceOperation("SendSms", "sms-container", env),
                                 GetHttpServiceOperation("GetUserMessages"),
                                 GetHttpServiceOperation("GetEmailMessageDetail"),
                                 GetHttpServiceOperation("GetSmsMessageDetail")
                             }
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "MESSAGING-SERVICE-KEY" },
                    {"ServiceSecret", "MESSAGING-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Messaging", messagingConfiguration);

            var auditConfiguration = new ServiceMetaData
            {
                Name = "Audit",
                Endpoints = new List<ServiceEndpoint>
                {
                    new ServiceEndpoint
                    {
                        IsPrimary = true,
                        Uri = "http://localhost:5835/api/{0}",
                        Type = ServiceEndpointType.AZF,
                        Operations = new List<ServiceOperation>
                        {
                            GetHttpServiceOperation("Ping"),
                            GetQueueServiceOperation("SaveAuditItem", "audit-item", env),
                            GetHttpServiceOperation("Search"),
                            GetHttpServiceOperation("CheckUserAgent"),
                            GetHttpServiceOperation("CheckIpAddress")
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "AUDIT-SERVICE-KEY" },
                    {"ServiceSecret", "AUDIT-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Audit", auditConfiguration);

            var identityConfiguration = new ServiceMetaData
            {
                Name = "Identity",
                Endpoints = new List<ServiceEndpoint>
                {
                    new ServiceEndpoint
                    {
                        IsPrimary = true,
                        Uri = "http://localhost:5845/api/{0}",
                        Type = ServiceEndpointType.AZF,
                        Operations = new List<ServiceOperation>
                        {
                            GetHttpServiceOperation("Ping"),
                            GetHttpServiceOperation("SaveCredential"),
                            GetHttpServiceOperation("GetCredentialById", false, true, "Id", true),
                            GetHttpServiceOperation("GetCredentialByEmail", false, true, "Email", true),
                            GetHttpServiceOperation("CheckEmail", false, true, "Email", true),
                            GetHttpServiceOperation("ResetAccessFailedCounter"),
                            GetHttpServiceOperation("Verify"),
                            GetHttpServiceOperation("Validate"),
                            GetHttpServiceOperation("SaveMobile"),
                            GetHttpServiceOperation("SaveAlternateEmail"),
                            GetHttpServiceOperation("GetVerificationByIdentityId"),
                            GetHttpServiceOperation("GetVerificationTrackingByVerificationGlobalId"),
                            GetHttpServiceOperation("SaveProfile"),
                            GetHttpServiceOperation("GetProfile"),
                            GetHttpServiceOperation("GetProfileList"),
                            GetHttpServiceOperation("EnrolUser"),
                            GetHttpServiceOperation("VerifyToken"),
                            GetQueueServiceOperation("SendPasswordResetMessage", "identity-passwordreset", "local")
                        }
                    }
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "IDENTITY-SERVICE-KEY" },
                    {"ServiceSecret", "IDENTITY-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Identity", identityConfiguration);

            var portalConfiguration = new ServiceMetaData
            {
                Name = "Portal",
                Endpoints = new List<ServiceEndpoint>
                {
                    new ServiceEndpoint
                    {
                        IsPrimary = true,
                        Uri = "http://localhost:5855/api/{0}",
                        Type = ServiceEndpointType.AZF,
                        Operations = new List<ServiceOperation>
                        {
                            GetHttpServiceOperation("Ping"),
                            GetHttpServiceOperation("SaveUserInfo"),
                            GetHttpServiceOperation("GetUserInformationByIdentityId"),
                            GetHttpServiceOperation("OnboardOrganisation"),
                            GetHttpServiceOperation("SaveOrganisation"),
                            GetHttpServiceOperation("GetOrganisationByUserGlobalId"),
                            GetHttpServiceOperation("GetOrganisationByGlobalId"),
                            GetHttpServiceOperation("GetApplicationListByOrganisationGlobalId"),
                            GetHttpServiceOperation("CreateApplication"),
                            GetHttpServiceOperation("SaveApplication"),
                            GetHttpServiceOperation("GetApplicationByGlobalId"),
                            GetHttpServiceOperation("GetApplicationByKey"),
                            GetHttpServiceOperation("SaveApplicationConfiguration")
                        }
                    }           
                },
                Keys = new Dictionary<string, string>
                {
                    {"ServiceKey", "PORTAL-SERVICE-KEY" },
                    {"ServiceSecret", "PORTAL-SERVICE-SECRET" }
                }
            };

            dictionary.Add("Portal", portalConfiguration);

            return dictionary;
        }

        public ServiceOperation GetHttpServiceOperation(string name, bool cacheEnabled = false, bool perRequest = false, string perRequestIdentifier = "", bool returnOnNull = false)
        {
            var serviceOperation = new ServiceOperation
            {
                Name = name,
                Protocol = ServiceEndpointProtocol.HTTP,
                Parameters = new Dictionary<string, string>
                {
                    { "METHOD","POST" },
                    { "AUTHORIZE","false" }
                },
            };

            if (cacheEnabled)
            {
                serviceOperation.Cache = new OperationCacheSettings
                {
                    CacheType = OperationCacheType.File,
                    Enabled = true,
                    FileKey = perRequest ? "clientdata/{MethodName}/{Id}.cache" : "clientdata/{MethodName}.cache",
                    PerRequestIdentifier = perRequestIdentifier,
                    PerRequest = perRequest,
                    ReturnOnNull = returnOnNull
                };
            }

            return serviceOperation;
        }

        public ServiceOperation GetQueueServiceOperation(string operationName, string queueName, string env)
        {
            return new ServiceOperation
            {
                Name = operationName,
                Protocol = ServiceEndpointProtocol.QUEUE,
                Parameters = new Dictionary<string, string>()
                {
                    { "QUEUENAME", queueName }
                }
            };
        }

        [Test]
        public void TestSaveBinaryFile()
        {
            var filePath = "aafb177a59cf44fc/email/img/default-email-header-logo.png";
            var container = "Assets";

            var image = CreateBitmapImage("Acorn Industries");
            using var myStream = new MemoryStream();
            image.Save(myStream, System.Drawing.Imaging.ImageFormat.Png);
            myStream.Position = 0;
            _fileStore.SaveBinaryFile(myStream, container, "clnt", "au", "est", filePath, "image/png");
        }

//        [Test]
//        public void TestAppendFile()
//        {
//            var container = "Telemetry";
//            var start = DateTime.UtcNow;
//            Dictionary<string,bool> fileState = new Dictionary<string, bool>();

//            for (var i = 0; i < 1000; i++)
//            {
//                for (var j = 0; j < 50; j++)
//                {
//                    var end = DateTime.UtcNow;

//                    var telemetryEvent = new TelemetryEvent
//                    {
//                        Controller = "Identity",
//                        Action = "Login",
//                        InError = false,
//                        ErrorType = "UNLIKELY",
//                        Messages = "NONE",
//                        TickDuration = end.Ticks - start.Ticks,
//                        CaptureStartDate = start,
//                        CaptureEndDate = end
//                    };

//                    var serializedEvent = JsonConvert.SerializeObject(telemetryEvent);

//                    using var stream = new MemoryStream();
//                    using var writer = new StreamWriter(stream);
//                    writer.Write(serializedEvent + "\n");
//                    writer.Flush();
//                    stream.Position = 0;

//                    var cdt = DateTime.UtcNow;
//                    var filePath = $"utdata/1/{cdt.Year}/{cdt.Month}/{cdt.Day}/{cdt.Hour}/{cdt.Minute}/data.json";

//                    var create = false;
//                    if (!fileState.ContainsKey(filePath))
//                    {
//                        create = true;
//                        fileState.Add(filePath, true);
//                    }

//                    _fileStore.AppendBinaryFile(stream, container, "mgmt", "au", "est", filePath, create);
//                }

////                Thread.Sleep(20);
//            }
//        }

        public Bitmap CreateBitmapImage(string imageText)
        {
            var bmpImage = new Bitmap(2, 2);

            // Create the Font object for the image text drawing.
            var font = new Font("Rockwell", 45, FontStyle.Bold, GraphicsUnit.Pixel);

            // Create a graphics object to measure the text's width and height.
            var objGraphics = Graphics.FromImage(bmpImage);

            // This is where the bitmap size is determined.
            var intWidth = (int)objGraphics.MeasureString(imageText, font).Width;
            var intHeight = (int)objGraphics.MeasureString(imageText, font).Height;

            // Create the bmpImage again with the correct size for the text and font.
            bmpImage = new Bitmap(bmpImage, new Size(intWidth, intHeight));

            // Add the colors to the new bitmap.
            objGraphics = Graphics.FromImage(bmpImage);

            // Set Background color
            objGraphics.Clear(Color.Transparent);
            objGraphics.SmoothingMode = SmoothingMode.HighQuality;

            objGraphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            objGraphics.CompositingQuality = CompositingQuality.HighQuality;
            objGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            objGraphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;

            objGraphics.DrawString(imageText, font, new SolidBrush(Color.Black), 0, 0, StringFormat.GenericTypographic);

            objGraphics.Flush();

            return bmpImage;
        }

        [Test]
        public void TestLoadFile()
        {
            var filePath = "clientdata/getcountrylist.cache";
            var container = "Portal";
            var content = _fileStore.LoadFile<object>(container, "mgmt", "au", "est", filePath);
        }
    }
}