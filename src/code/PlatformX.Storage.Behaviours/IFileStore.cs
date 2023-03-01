using System.IO;
using System.Threading.Tasks;

namespace PlatformX.Storage.Behaviours
{
    public interface IFileStore
    {
        TResponse LoadClientFile<TResponse>(string serviceName, string filePath, string regionKey, string locationKey);

        TResponse LoadFile<TResponse>(string serviceName, string roleKey, string regionKey, string locationKey, string filePath);

        Task<MemoryStream> LoadFileAsStream(string serviceName, string roleKey, string regionKey, string locationKey, string filePath);

        string LoadFileAsString(string serviceName, string roleKey, string regionKey, string locationKey,
            string filePath);

        void SaveClientFile<TData>(TData data, string serviceName, string filePath, string contentType, string regionKey,
            string locationKey);

        void SaveFile<TData>(TData data, string serviceName, string roleKey, string regionKey, string locationKey, string filePath, string contentType);

        void CopyClientFolder(string serviceName, string sourceFolder, string searchKey, string replaceKey,
            string regionKey, string locationKey);

        void CopyFolder(string serviceName, string sourceFolder, string searchKey, string replaceKey);

        void SaveBinaryFile(Stream stream, string serviceName, string roleKey, string regionKey, string locationKey,
            string filePath, string contentType);

        void AppendBinaryFile(Stream stream, string serviceName, string roleKey, string regionKey, string locationKey,
            string filePath, bool create);

        void MoveClientFile(string serviceName, string sourceFile, string targetFile, string regionKey, string locationKey);

        void MoveFile(string serviceName, string sourceFile, string targetFile);
    }
}
