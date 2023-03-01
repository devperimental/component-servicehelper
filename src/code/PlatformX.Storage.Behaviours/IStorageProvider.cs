using PlatformX.Storage.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace PlatformX.Storage.Behaviours
{
    public interface IStorageProvider
    {
        Task DeleteFile(StorageDefinition storageDefinition, string filePath);
        Task<string> LoadFile(StorageDefinition storageDefinition, string filePath);
        Task<MemoryStream> LoadFileAsStream(StorageDefinition storageDefinition, string filePath);
        Task<bool> MoveFile(StorageDefinition storageDefinitionSource, StorageDefinition storageDefinitionTarget, string sourcePath, string targetPath);
        Task SaveFile(StorageDefinition storageDefinition, Stream stream, string filePath, string contentType);
        Task<List<string>> GetFiles(StorageDefinition storageDefinition, string folderPath, int? segmentSize);
        Task<bool> CopyFile(StorageDefinition storageDefinitionSource, StorageDefinition storageDefinitionTarget,
            string sourcePath, string targetPath);
        Task AppendToFile(StorageDefinition storageDefinition, Stream stream, string filePath, bool create);
    }
}
