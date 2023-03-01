using PlatformX.Messaging.Behaviours;
using PlatformX.Messaging.Types.EnumTypes;
using System;
using System.Security.Cryptography;
using System.Text;

namespace PlatformX.Messaging.Helper
{
    public class HashGenerationHelper : IHashGeneration
    {
        private const string charSet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890-_";
        public string GenerateRequestInput(string serviceKey, string serviceTimestamp, string serviceSecret, string ipAddress, string correlationId)
        {
            var stringToHash = string.Format($"{serviceKey}:{serviceTimestamp}:{ipAddress}:{correlationId}:{serviceSecret}");
            return CreateHash(stringToHash);
        }

        public string CreateHash(string stringToHash, HashType type = HashType.SHA512)
        {
            var hashedString = "";

            switch (type)
            {
                case HashType.SHA512:
                    hashedString = CreateHashSHA512(stringToHash);
                    break;
                case HashType.SHA256:
                    hashedString = CreateHashSHA256(stringToHash);
                    break;
            }

            return hashedString;
        }

        private static string CreateHashSHA512(string token)
        {
            using (var hash = SHA512.Create())
            {
                var enc = Encoding.UTF8;
                var result = hash.ComputeHash(enc.GetBytes(token));

                return Convert.ToBase64String(result);
            }
        }

        private static string CreateHashSHA256(string token)
        {
            using (var hash = SHA256.Create())
            {
                var enc = Encoding.UTF8;
                var result = hash.ComputeHash(enc.GetBytes(token));

                return Convert.ToBase64String(result);
            }
        }

        public static string GetUniqueToken(int tokenLength)
        {
            using (var crypto = new RNGCryptoServiceProvider())
            {
                var data = new byte[tokenLength];

                byte[] buffer = null;
                var maxRandom = byte.MaxValue - ((byte.MaxValue + 1) % charSet.Length);

                crypto.GetBytes(data);

                var result = new char[tokenLength];

                for (int i = 0; i < tokenLength; i++)
                {
                    var value = data[i];

                    while (value > maxRandom)
                    {
                        if (buffer == null)
                        {
                            buffer = new byte[1];
                        }

                        crypto.GetBytes(buffer);
                        value = buffer[0];
                    }

                    result[i] = charSet[value % charSet.Length];
                }

                return new string(result);
            }
        }
    }
}
