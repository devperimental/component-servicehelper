using Azure.Identity;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using PlatformX.Storage.Behaviours;
using PlatformX.Storage.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
//using PlatformX.Common.Types.DataContract;
using Azure;

namespace PlatformX.Storage.Azure
{
    public class AzureStorage<TLog> : IStorageProvider
    {
        //private readonly BootstrapConfiguration _bootstrapConfig;
        private readonly ILogger<TLog> _traceLogger;
        private readonly Dictionary<string,BlobContainerClient> _containerClientList;
        private static readonly object LockObject = new object();
        private bool _refreshCredentials = false;
        private string _environment;

        //https://github.com/Azure/azure-sdk-for-net/tree/master/sdk/storage/Azure.Storage.Blobs/samples
        public AzureStorage(string environment, ILogger<TLog> traceLogger)
        {
            _traceLogger = traceLogger;
            _environment = environment;
            _containerClientList = new Dictionary<string, BlobContainerClient>();
        }

        private BlobContainerClient GetClient(StorageDefinition storageDefinition)
        {
            if (string.IsNullOrEmpty(storageDefinition.AccountName))
            {
                throw new ArgumentNullException("AccountName", "AccountName is null in StorageDefinition");
            }

            var containerName = storageDefinition.ContainerName.ToLower();

            if (string.IsNullOrEmpty(containerName))
            {
                throw new ArgumentNullException("containerName", "containerName is null in StorageDefinition");
            }

            // Construct the blob container endpoint from the arguments.
            var containerEndpoint = $"https://{storageDefinition.AccountName}.blob.core.windows.net/{containerName}";

            var containerKey = $"{storageDefinition.AccountName.ToLower()}{storageDefinition.ContainerName.ToLower()}";
            if (_containerClientList.ContainsKey(containerKey) && !_refreshCredentials)
            {
                return _containerClientList[containerKey];
            }

            lock (LockObject)
            {
                if (!_containerClientList.ContainsKey(containerKey))
                {
                    if (_environment == "local")
                    {
                        var credential = new VisualStudioCredential(new VisualStudioCredentialOptions { TenantId = storageDefinition.TenantId });
                        _containerClientList[containerKey] = new BlobContainerClient(new Uri(containerEndpoint), credential);
                    }
                    else
                    {
                        var credential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { SharedTokenCacheTenantId = storageDefinition.TenantId });
                        _containerClientList[containerKey] = new BlobContainerClient(new Uri(containerEndpoint), credential);
                    }
                    _refreshCredentials = false;
                }
            }
            
            return _containerClientList[containerKey];
        }

        public async Task DeleteFile(StorageDefinition storageDefinition, string filePath)
        {
            try
            {
                var blobClient = GetClient(storageDefinition);
                var blockBlob = blobClient.GetBlobClient(filePath);
                await blockBlob.DeleteIfExistsAsync();
            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file move");
                _refreshCredentials = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error deleting file from {filePath}");
            }
        }

        public async Task<List<string>> GetFiles(StorageDefinition storageDefinition, string folderPath, int? pageSize)
        {
            string continuationToken = null;
            var blobList = new List<string>();
            try
            {
                // Call the listing operation and enumerate the result segment.
                // When the continuation token is empty, the last segment has been returned
                // and execution can exit the loop.
                var blobContainerClient = GetClient(storageDefinition);

                do
                {
                    var resultSegment = blobContainerClient.GetBlobs(prefix: folderPath)
                        .AsPages(continuationToken, pageSize);

                    foreach (var blobPage in resultSegment)
                    {
                        blobList.AddRange(blobPage.Values.Select(blobItem => blobItem.Name));
                        continuationToken = blobPage.ContinuationToken;
                    }

                } while (continuationToken != "");

            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file list operation");
                _refreshCredentials = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error listing files for path {folderPath}");
            }

            return await Task.FromResult(blobList);
        }

        public async Task<MemoryStream> LoadFileAsStream(StorageDefinition storageDefinition, string filePath)
        {
            var blobContainerClient = GetClient(storageDefinition);
            var blobClient = blobContainerClient.GetBlobClient(filePath);

            var memStream = new MemoryStream();

            try
            {
                await blobClient.DownloadToAsync(memStream);
            }
            catch (FileNotFoundException ex)
            {
                _traceLogger.LogError(ex, "Error downloading file - Not Found");
            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file download");
                _refreshCredentials = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, "Error downloading file");
            }
            
            return memStream;
        }

        public async Task<string> LoadFile(StorageDefinition storageDefinition, string filePath)
        {
            var output = string.Empty;

            var blobContainerClient = GetClient(storageDefinition);
            var blobClient = blobContainerClient.GetBlobClient(filePath);

            try
            {
                using (var memStream = new MemoryStream())
                {
                    await blobClient.DownloadToAsync(memStream);

                    memStream.Position = 0;
                    using (var sr = new StreamReader(memStream))
                    {
                        output = await sr.ReadToEndAsync();
                    }
                }
            }
            catch(FileNotFoundException ex)
            {
                _traceLogger.LogError(ex, "Error downloading file - Not Found");
            }
            catch(AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file download");
                _refreshCredentials = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, "Error downloading file");
            }
        
            return output;
        }

        public async Task<bool> MoveFile(StorageDefinition storageDefinitionSource, StorageDefinition storageDefinitionTarget, string sourcePath, string targetPath)
        {
            try
            {
                var blobClientSource = GetClient(storageDefinitionSource);
                var sourceBlob = blobClientSource.GetBlobClient(sourcePath);

                var blobClientTarget = GetClient(storageDefinitionTarget);
                var targetBlob = blobClientTarget.GetBlobClient(targetPath);

                await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                await sourceBlob.DeleteAsync();

                return true;
            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file move");
                _refreshCredentials = true;
            }
            catch (RequestFailedException rfex)
            {
                _traceLogger.LogError(rfex, $"Error moving file from {storageDefinitionSource.ContainerName}:{sourcePath} to {storageDefinitionTarget.ContainerName}:{targetPath}");
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error moving file from {storageDefinitionSource.ContainerName}:{sourcePath} to {storageDefinitionTarget.ContainerName}:{targetPath}");
            }
            return false;
        }

        public async Task<bool> CopyFile(StorageDefinition storageDefinitionSource, StorageDefinition storageDefinitionTarget, string sourcePath, string targetPath)
        {
            try
            {
                var blobClientSource = GetClient(storageDefinitionSource);
                var sourceBlob = blobClientSource.GetBlobClient(sourcePath);

                var blobClientTarget = GetClient(storageDefinitionTarget);
                var targetBlob = blobClientTarget.GetBlobClient(targetPath);

                await targetBlob.StartCopyFromUriAsync(sourceBlob.Uri);
                return true;
            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file move");
                _refreshCredentials = true;
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error copying file from {sourcePath} to {targetPath}");
            }
            return false;
        }

        public async Task SaveFile(StorageDefinition storageDefinition, Stream stream, string filePath, string contentType)
        {
            try
            {
                var containerClient = GetClient(storageDefinition);

                var blobClient = containerClient.GetBlobClient(filePath);

                //try
                //{
                //    if (await blobClient.ExistsAsync())
                //    {
                //        await blobClient.DeleteAsync();
                //    }
                //}
                //catch (Exception ex)
                //{
                //    _traceLogger.LogError(ex, $"Error attempting check and delete for {filePath}");
                //}

                var blobHttpHeader = new BlobHttpHeaders { ContentType = contentType };

                await blobClient.UploadAsync(stream, blobHttpHeader);
            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file download");
                _refreshCredentials = true;
            }
            catch (RequestFailedException rfex)
            {
                //https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
                if (rfex.ErrorCode == "ContainerNotFound")
                {
                    throw new ApplicationException($"Container:{storageDefinition.ContainerName} not found", rfex);
                }
                else
                {
                    _traceLogger.LogError(rfex, $"Error saving {filePath}");
                }
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error saving {filePath}");
            }
        }

        public async Task AppendToFile(StorageDefinition storageDefinition, Stream stream, string filePath, bool create)
        {
            try
            {
                var containerClient = GetClient(storageDefinition);

                var appendBlobClient = containerClient.GetAppendBlobClient(filePath);

                if (create)
                {
                    var blobHttpHeader = new BlobHttpHeaders { ContentType = "application/octet" };
                    await appendBlobClient.CreateAsync(blobHttpHeader);
                }
                await appendBlobClient.AppendBlockAsync(stream);

            }
            catch (AuthenticationFailedException ex)
            {
                _traceLogger.LogError(ex, "Error authenticating file download");
                _refreshCredentials = true;
            }
            catch (RequestFailedException rfex)
            {
                //https://docs.microsoft.com/en-us/rest/api/storageservices/blob-service-error-codes
                if (rfex.ErrorCode == "ContainerNotFound")
                {
                    throw new ApplicationException($"Container:{storageDefinition.ContainerName} not found", rfex);
                }
                else
                {
                    _traceLogger.LogError(rfex, $"Error saving {filePath}");
                }
            }
            catch (Exception ex)
            {
                _traceLogger.LogError(ex, $"Error appending to {filePath}");
            }
        }
    }
}
