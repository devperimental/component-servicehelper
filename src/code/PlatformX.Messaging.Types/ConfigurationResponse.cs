using PlatformX.ServiceLayer.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformX.Messaging.Types
{
    public class ConfigurationResponse : GenericResponse
    {
        public ConfigurationResponse() { }
        public int Count { get; set; }
        public Dictionary<string, string> Configuration { get; set; }
        public Dictionary<string, ServiceMetaData> ServiceMetaDataList { get; set; }
    }
}
