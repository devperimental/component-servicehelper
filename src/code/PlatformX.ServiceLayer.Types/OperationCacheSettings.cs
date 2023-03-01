using System.Collections.Generic;

namespace PlatformX.ServiceLayer.Types
{
    public class OperationCacheSettings
    {
        public bool Enabled { get; set; }
        public string CacheType { get; set; }
        public string FileKey { get; set; }
        public bool PerRequest { get; set; }
        public string PerRequestIdentifier { get; set; }
        public bool ReturnOnNull { get; set; }
    }
}