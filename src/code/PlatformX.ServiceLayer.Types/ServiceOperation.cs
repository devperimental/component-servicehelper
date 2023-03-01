using System.Collections.Generic;

namespace PlatformX.ServiceLayer.Types
{
    public class ServiceOperation
    {
        public string Name { get; set; }
        public string Protocol { get; set; }
        public OperationCacheSettings Cache { get; set; }
        public Dictionary<string, string> Parameters { get; set; } 
    }
}