using Newtonsoft.Json;
using PlatformX.Common.Types.DataContract;
using PlatformX.Storage.Behaviours;
using PlatformX.Storage.Types;
using System;
using System.IO;
using PlatformX.Settings.Behaviours;
using PlatformX.Messaging.Types.Constants;
using System.Threading.Tasks;

namespace PlatformX.Storage.StoreClient
{
    public class FileStore : IFileStore
    {
        private readonly IStorageProvider _storageProvider;
        private readonly BootstrapConfiguration _bootstrapConfiguration;
        private readonly IEndpointHelper _endpointHelper;

        public FileStore(IStorageProvider storageProvider, 
            BootstrapConfiguration bootstrapConfiguration,
            IEndpointHelper endpointHelper)
        {
            _storageProvider = storageProvider ?? throw new ArgumentNullException(nameof(storageProvider), "storageProvider not defined in FileStore constructor");
            _bootstrapConfiguration = bootstrapConfiguration ?? throw new ArgumentNullException(nameof(bootstrapConfiguration), "bootstrapConfiguration not defined in FileStore constructor");
            _endpointHelper = endpointHelper ?? throw new ArgumentNullException(nameof(endpointHelper), "endpointHelper not defined in FileStore constructor");

            if (string.IsNullOrEmpty(_bootstrapConfiguration.TenantId))
            {
                throw new ArgumentNullException(nameof(_bootstrapConfiguration.TenantId), "TenantId not defined in configuration");
            }
        }

        public TResponse LoadClientFile<TResponse>(string serviceName, string filePath, string regionKey, string locationKey)
        {
            var storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            return LoadFileInternal<TResponse>(serviceName, filePath, storageAccountName);
        }

        public TResponse LoadFile<TResponse>(string serviceName, string roleKey, string regionKey, string locationKey, string filePath)
        {
            string storageAccountName;
            if (roleKey == SystemRoleKey.Management)
            {
                storageAccountName = _endpointHelper.GetManagementStorageAccount(regionKey, locationKey);
            }
            else if (roleKey == SystemRoleKey.Client)
            {
                storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            }
            else
            {
                storageAccountName = _endpointHelper.GetStorageAccount();
            }

            return LoadFileInternal<TResponse>(serviceName, filePath, storageAccountName);
        }

        public string LoadFileAsString(string serviceName, string roleKey, string regionKey, string locationKey, string filePath)
        {
            var storageDefinition = GetStorageDefinition(serviceName, roleKey, regionKey, locationKey);
            return _storageProvider.LoadFile(storageDefinition, filePath).Result;
        }

        public async Task<MemoryStream> LoadFileAsStream(string serviceName, string roleKey, string regionKey, string locationKey, string filePath)
        {
            var storageDefinition = GetStorageDefinition(serviceName, roleKey, regionKey, locationKey);
            return await _storageProvider.LoadFileAsStream(storageDefinition, filePath);
        }

        private StorageDefinition GetStorageDefinition(string serviceName, string roleKey, string regionKey, string locationKey)
        {
            string storageAccountName;
            if (roleKey == SystemRoleKey.Management)
            {
                storageAccountName = _endpointHelper.GetManagementStorageAccount(regionKey, locationKey);
            }
            else if (roleKey == SystemRoleKey.Client)
            {
                storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            }
            else
            {
                storageAccountName = _endpointHelper.GetStorageAccount();
            }

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            return storageDefinition;
        }

        private TResponse LoadFileInternal<TResponse>(string serviceName, string filePath, string storageAccountName)
        {
            var response = default(TResponse);

            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in LoadFile");
            }

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            var data = _storageProvider.LoadFile(storageDefinition, filePath).Result;

            if (!string.IsNullOrEmpty(data))
            {
                response = JsonConvert.DeserializeObject<TResponse>(data);
            }
            return response;
        }

        public void SaveClientFile<TData>(TData data, string serviceName, string filePath, string contentType, string regionKey, string locationKey)
        {
            var storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            SaveFileInternal(data, serviceName, filePath, contentType, storageAccountName);
        }

        public void SaveFile<TData>(TData data, string serviceName, string roleKey, string regionKey, string locationKey, string filePath, string contentType)
        {
            string storageAccountName;
            if (roleKey == SystemRoleKey.Management)
            {
                storageAccountName = _endpointHelper.GetManagementStorageAccount(regionKey, locationKey);
            }
            else if (roleKey == SystemRoleKey.Client)
            {
                storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            }
            else
            {
                storageAccountName = _endpointHelper.GetStorageAccount();
            }

            SaveFileInternal(data, serviceName, filePath, contentType, storageAccountName);
        }

        private void SaveFileInternal<TData>(TData data, string serviceName, string filePath, string contentType, string storageAccountName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in SaveFile");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File Path not defined in SaveFile");
            }

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            var json = JsonConvert.SerializeObject(data);

            using (var stream = new MemoryStream())
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(json);
                writer.Flush();
                stream.Position = 0;

                _storageProvider.SaveFile(storageDefinition, stream, filePath, contentType).Wait();
            }
        }

        private void CopyFolderInternal(string serviceName, string sourceFolder, string searchKey, string replaceKey, string storageAccountName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in LoadFile");
            }

            //var sourceFolder = "mail-template/client";

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            var files = _storageProvider.GetFiles(storageDefinition, sourceFolder, 50).Result;

            foreach (var file in files)
            {
                var newFileName = file.Replace(searchKey, replaceKey);
                var complete = _storageProvider.CopyFile(storageDefinition, storageDefinition, file, newFileName).Result;
            }
        }

        public void CopyClientFolder(string serviceName, string sourceFolder, string searchKey, string replaceKey, string regionKey, string locationKey)
        {
            var storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            CopyFolderInternal(serviceName, sourceFolder, searchKey, replaceKey, storageAccountName);
        }

        public void CopyFolder(string serviceName, string sourceFolder, string searchKey, string replaceKey)
        {
            var storageAccountName = _endpointHelper.GetStorageAccount();
            CopyFolderInternal(serviceName, sourceFolder, searchKey, replaceKey, storageAccountName);
        }

        private void MoveFileInternal(string serviceName, string sourceFile, string targetFile, string storageAccountName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in MoveFileInternal");
            }

            //var sourceFolder = "mail-template/client";

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            var fileMoved = _storageProvider.MoveFile(storageDefinition, storageDefinition, sourceFile, targetFile).Result;
        }

        public void MoveClientFile(string serviceName, string sourceFile, string targetFile, string regionKey, string locationKey)
        {
            var storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
            MoveFileInternal(serviceName, sourceFile, targetFile, storageAccountName);
        }

        public void MoveFile(string serviceName, string sourceFile, string targetFile)
        {
            var storageAccountName = _endpointHelper.GetStorageAccount();
            MoveFileInternal(serviceName, sourceFile, targetFile, storageAccountName);
        }

        public void SaveBinaryFile(Stream stream, string serviceName, string roleKey, string regionKey, string locationKey, string filePath, string contentType)
        {
            string storageAccountName;

            switch (roleKey)
            {
                case SystemRoleKey.Management:
                    storageAccountName = _endpointHelper.GetManagementStorageAccount(regionKey, locationKey);
                    break;
                case SystemRoleKey.Client:
                    storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
                    break;
                default:
                    storageAccountName = _endpointHelper.GetStorageAccount();
                    break;
            }

            SaveBinaryFileInternal(stream, serviceName, filePath, contentType, storageAccountName);
        }

        private void SaveBinaryFileInternal(Stream stream, string serviceName, string filePath, string contentType, string storageAccountName)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in SaveBinaryFileInternal");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File Path not defined in SaveBinaryFileInternal");
            }

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            stream.Position = 0;
            _storageProvider.SaveFile(storageDefinition, stream, filePath, contentType).Wait();

        }

        public void AppendBinaryFile(Stream stream, string serviceName, string roleKey, string regionKey, string locationKey, string filePath, bool create)
        {
            string storageAccountName;

            switch (roleKey)
            {
                case SystemRoleKey.Management:
                    storageAccountName = _endpointHelper.GetManagementStorageAccount(regionKey, locationKey);
                    break;
                case SystemRoleKey.Client:
                    storageAccountName = _endpointHelper.GetClientStorageAccount(regionKey, locationKey);
                    break;
                default:
                    storageAccountName = _endpointHelper.GetStorageAccount();
                    break;
            }

            AppendBinaryFileInternal(stream, serviceName, filePath, storageAccountName, create);
        }

        private void AppendBinaryFileInternal(Stream stream, string serviceName, string filePath, string storageAccountName, bool create)
        {
            if (string.IsNullOrEmpty(serviceName))
            {
                throw new ArgumentNullException(nameof(serviceName), "Service Name not defined in AppendBinaryFileInternal");
            }

            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath), "File Path not defined in AppendBinaryFileInternal");
            }

            var storageDefinition = new StorageDefinition
            {
                AccountName = storageAccountName,
                TenantId = _bootstrapConfiguration.TenantId,
                ContainerName = serviceName.ToLower()
            };

            _storageProvider.AppendToFile(storageDefinition, stream, filePath, create).Wait();

        }
    }
}
