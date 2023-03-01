using NUnit.Framework;
using PlatformX.Messaging.Helper;

namespace PlatformX.Messaging.NTesting
{
    public class MessagingTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void TestHashGeneration()
        {
            var hashGenerationHelper = new HashGenerationHelper();
            //var hashOutcome = "t5lcSDzNd05GCt3CKV3qXh5OUOEnt/VQmEItNVm7sHZnTHrY0SaGJQdDuI094c8sI4+pHbP+1EECVZZm1iP4RQ==";
            var portalName = "ARCHITECTED";
            var serviceTimestamp = "637209686814252385";
            //var applicationServiceKey = "523BCF20-CFA5-4092-A900-75BF341A2D0B";
            var serviceSecret = "5C9A09EA-7A81-4B45-88F3-63C2F718E5AB";
            var ipAddress = "1.2.3.4";
            var correlationId = "C4EDEF4F-F4DE-4C49-80D6-8B94C16C6390";

            var requestHash = hashGenerationHelper.GenerateRequestInput(portalName, serviceTimestamp, serviceSecret, ipAddress, correlationId);

            Assert.IsTrue(!string.IsNullOrEmpty(requestHash));
        }

        [Test]
        public void TestHashGeneration256()
        {
            var hashGenerationHelper = new HashGenerationHelper();
            var valToHash = "12345678901234567890";
            var cryptoJSHash = "btZF7w4avqG/Hk6TX/BPnhjTmBI4f2PNo0FbRiQPBAU=";
            var requestHash = hashGenerationHelper.CreateHash(valToHash, Types.EnumTypes.HashType.SHA256);

            Assert.IsTrue(cryptoJSHash == requestHash);
        }


        [Test]
        public void TestGenerateRandomString()
        {
            var randomString = HashGenerationHelper.GetUniqueToken(50);
            var anotherRandomString = HashGenerationHelper.GetUniqueToken(50);
         
            Assert.IsTrue(randomString != anotherRandomString);
        }
    }
}