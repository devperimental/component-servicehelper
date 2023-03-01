using Microsoft.Extensions.Logging;
using PlatformX.Messaging.Types;
using System.Collections.Generic;
using System.Net.Http;

namespace PlatformX.ServiceLayer.Behaviours
{
    public interface IMicroServiceServerHelper
    {
        RequestContext GetRequestContext<T>(HttpRequestMessage req, ILogger<T> logger, bool bypassHashCheck = false);
        RequestContext GetRequestContext<T>(IDictionary<string, object> userProperties, ILogger<T> traceLogger, bool bypassHashCheck = false);
    }
}
