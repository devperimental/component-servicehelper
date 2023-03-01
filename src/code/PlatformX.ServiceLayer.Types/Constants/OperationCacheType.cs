using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformX.ServiceLayer.Types.Constants
{
    public static class OperationCacheType
    {
        public const string File = nameof(File);
        public const string MemoryCache = nameof(MemoryCache);
        public const string DistributedCache = nameof(DistributedCache);
    }
}
