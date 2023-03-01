using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using PlatformX.Messaging.Types;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace PlatformX.FunctionLayer.Behaviours
{
    public interface IFunctionHelper
    {
        Task<IActionResult> ExecuteHttpCall<T>(Func<RequestContext, string, Task<IActionResult>> func, HttpRequestMessage req, string controllerName, string callingMethod, ILogger<T> logger);
        void ExecuteQueueCall<T>(Action<RequestContext, string> action, Message message, string controllerName, string callingMethod, ILogger<T> traceLogger);
        void ExecuteTimerCall<T>(Action<RequestContext> func, string controllerName, string callingMethod, ILogger<T> traceLogger);
        
    }
}
