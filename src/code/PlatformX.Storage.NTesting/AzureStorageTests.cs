using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using PlatformX.Common.Types.DataContract;
using PlatformX.Storage.Azure;
using PlatformX.Storage.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PlatformX.Storage.NTesting
{
    [TestFixture]
    public class AzureStorageTests
    {
        private StorageDefinition _storageDefinition;
        private BootstrapConfiguration _bootstrapConfig = new BootstrapConfiguration { Environment = "local"};
        private AzureStorage<AzureStorageTests> _storage;
        [SetUp]
        public void Setup()
        {
            _storageDefinition = new StorageDefinition
            {
                AccountName = "dzappstorlocalmgmtauest",
                ContainerName = "testing",
                TenantId = Environment.GetEnvironmentVariable("PlatformDBTenantId"),
            };

            var traceLogger = new Mock<ILogger<AzureStorageTests>>();

            if (_storage == null)
            {
                _storage = new AzureStorage<AzureStorageTests>(_bootstrapConfig, traceLogger.Object);
            }
            
        }

        [Test]
        public async Task TestUpload()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("some text");
            writer.Flush();
            stream.Position = 0;

            var filePath = String.Format("ut/{0}/{1}/{2}/TestUpload-{3}.txt", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Guid.NewGuid().ToString());
            var contentType = "text/plain";

            await _storage.SaveFile(_storageDefinition, stream, filePath, contentType);
            stream.Position = 0;
            await _storage.SaveFile(_storageDefinition, stream, filePath, contentType);
        }

        [Test]
        public async Task TestMove()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("some text");
            writer.Flush();
            stream.Position = 0;

            var sourcefilePath = String.Format("ut/{0}/{1}/{2}/TestMove-{3}.txt", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Guid.NewGuid().ToString());
            var movedFilePath = String.Format("ut/{0}/{1}/{2}/moved/TestMove-{3}.txt", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Guid.NewGuid().ToString());
            var contentType = "text/plain";

            await _storage.SaveFile(_storageDefinition, stream, sourcefilePath, contentType);

            await _storage.MoveFile(_storageDefinition, _storageDefinition, sourcefilePath, movedFilePath);

            var fileContents = _storage.LoadFile(_storageDefinition, movedFilePath);

            Assert.IsNotNull(fileContents);
        }

        [Test]
        public async Task TestGetFiles()
        {
            var fileList = await GetFiles();
            Assert.IsNotNull(fileList);
        }

        public async Task<List<string>> GetFiles()
        {
            var sourceFolder = "mail-template/client";

            var storageDefinitionSource = new StorageDefinition
            {
                AccountName = "dzappstorlocalclntauest",
                ContainerName = "messaging",
                TenantId = Environment.GetEnvironmentVariable("PlatformDBTenantId"),
            };

            return await _storage.GetFiles(storageDefinitionSource, sourceFolder, 50);
        }

        [Test]
        public async Task TestCopyFiles()
        {
            var appKey = "abcdefgh";
            //var sourceFolder = "mail-template/client";
            //var targetFolder = "mail-template/{0}";

            var storageDefinition = new StorageDefinition
            {
                AccountName = "dzappstorlocalclntauest",
                ContainerName = "messaging",
                TenantId = Environment.GetEnvironmentVariable("PlatformDBTenantId"),
            };

            var storageDefinitionTarget = new StorageDefinition
            {
                AccountName = "dzappstorlocalclntauest",
                ContainerName = "messaging",
                TenantId = Environment.GetEnvironmentVariable("PlatformDBTenantId"),
            };

            var fileList = await GetFiles();

            foreach (var file in fileList)
            {
                var newFileName = file.Replace("client", appKey);
                await _storage.CopyFile(storageDefinition, storageDefinition, file, newFileName);
            }
        }

        [Test]
        public async Task TestDelete()
        {
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("some text");
            writer.Flush();
            stream.Position = 0;

            var sourcefilePath = String.Format("ut/{0}/{1}/{2}/TestDelete-{3}.txt", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Guid.NewGuid().ToString());
            var contentType = "text/plain";

            await _storage.SaveFile(_storageDefinition, stream, sourcefilePath, contentType);

            await _storage.DeleteFile(_storageDefinition, sourcefilePath);
        }

        [Test]
        public async Task TestDownload()
        {
            
            var stream = new MemoryStream();
            var writer = new StreamWriter(stream);
            writer.Write("some text");
            writer.Flush();
            stream.Position = 0;

            var sourcefilePath = String.Format("ut/{0}/{1}/{2}/TestDownload-{3}.txt", DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, Guid.NewGuid().ToString());
            var contentType = "text/plain";
            
            await _storage.SaveFile(_storageDefinition, stream, sourcefilePath, contentType);

            var fileContents = _storage.LoadFile(_storageDefinition, sourcefilePath);

            Assert.IsNotNull(fileContents);
        }

    }
}