using System.Collections.Generic;

namespace PlatformX.ServiceLayer.Types
{
    public class ServiceEndpoint
    {
        public bool IsPrimary { get; set; }
        public string Type { get; set; } // AZF // FN //
        public string Uri { get; set; }
        public string AccessKey { get; set; }
        public string FulfillmentRoleType { get; set; }
        public List<ServiceOperation> Operations{ get; set; }
    }
}