using System;
using System.Collections.Generic;

namespace PlatformX.ServiceLayer.Types
{
    public class ServiceMetaData    
    {
        public string Name { get; set; } // ServiceName
        public string HostEndpointType { get; set; } // API / AZF
        public string HostRoleType { get; set; } // clnt / mgmt
        public Dictionary<string, RoleTypeSetting> RoleTypeSettings { get; set; }
        public List<ServiceEndpoint> Endpoints { get; set; }
        public Dictionary<string, string> Keys { get; set; }
    }
}