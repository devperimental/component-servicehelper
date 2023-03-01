using PlatformX.Messaging.Types.EnumTypes;

namespace PlatformX.Messaging.Behaviours
{
    public interface IHashGeneration
    {
        string CreateHash(string stringToHash, HashType type);
        string GenerateRequestInput(string portalName, string serviceTimestamp, string serviceSecret, string ipAddress, string correlationId);
    }
}