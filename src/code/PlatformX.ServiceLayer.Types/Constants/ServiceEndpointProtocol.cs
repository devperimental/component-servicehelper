using System;
using System.Collections.Generic;
using System.Text;

namespace PlatformX.ServiceLayer.Types.Constants
{
    public static class ServiceEndpointProtocol
    {
        public const string HTTP = nameof(HTTP);
        public const string GRPC = nameof(GRPC);
        public const string QUEUE = nameof(QUEUE);
    }
}
