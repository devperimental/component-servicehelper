using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading.Tasks;
using PlatformX.Messaging.Types;
using PlatformX.FunctionLayer.Behaviours;
using Microsoft.Azure.ServiceBus;
using PlatformX.ServiceLayer.Behaviours;

namespace PlatformX.FunctionLayer.Helper
{
    public class FunctionHelper : IFunctionHelper
    {
        private readonly IMicroServiceServerHelper _microServiceServerHelper;
        
        public FunctionHelper(IMicroServiceServerHelper microServiceServerHelper)
        {
            _microServiceServerHelper = microServiceServerHelper ?? throw new ArgumentNullException(nameof(microServiceServerHelper));
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <param name="req"></param>
        /// <param name="controllerName"></param>
        /// <param name="callingMethod"></param>
        /// <param name="traceLogger"></param>
        /// <returns></returns>
        public async Task<IActionResult> ExecuteHttpCall<T>(Func<RequestContext, string, Task<IActionResult>> func, 
            HttpRequestMessage req, 
            string controllerName, 
            string callingMethod, 
            ILogger<T> traceLogger)
        {
            try
            {
                var requestContext = _microServiceServerHelper.GetRequestContext(req, traceLogger);

                if (requestContext.ResponseCode != (int)HttpStatusCode.OK)
                {
                    traceLogger.LogWarning("Non OK response code returned");
                    traceLogger.LogWarning(requestContext.ResponseContent);
                    return new BadRequestObjectResult(requestContext) { StatusCode = requestContext.ResponseCode, Value = requestContext.ResponseContent };
                }

                var requestJson = string.Empty;
                if (req.Content != null)
                {
                    requestJson = await req.Content.ReadAsStringAsync();
                }

                return await func(requestContext, requestJson);
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                return new BadRequestObjectResult(ex.Message) { StatusCode = (int)HttpStatusCode.InternalServerError };
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="message"></param>
        /// <param name="callingMethod"></param>
        /// <param name="traceLogger"></param>
        public void ExecuteQueueCall<T>(Action<RequestContext, string> action, 
            Message message,
            string controllerName,
            string callingMethod,
            ILogger<T> traceLogger)
        {
            try
            {
                var requestContext = _microServiceServerHelper.GetRequestContext(message.UserProperties, traceLogger);

                if (requestContext.ResponseCode != (int)HttpStatusCode.OK)
                {
                    traceLogger.LogWarning("Non OK response code returned");
                    traceLogger.LogWarning(requestContext.ResponseContent);
                    throw new UnauthorizedException($"Error processing message: {message.MessageId} - ResponseCode: {requestContext.ResponseCode} - Error: {requestContext.ResponseContent}");
                }

                var requestJson = System.Text.Encoding.UTF8.GetString(message.Body);

                action(requestContext, requestJson);
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="action"></param>
        /// <param name="controllerName"></param>
        /// <param name="callingMethod"></param>
        /// <param name="traceLogger"></param>
        public void ExecuteTimerCall<T>(Action<RequestContext> action,
            string controllerName,
            string callingMethod,
            ILogger<T> traceLogger)
        {
            try
            {

                var correlationId = Guid.NewGuid().ToString();
                var apiCallContext = new RequestContext
                {
                    IpAddress = GetLocalIpAddress(),
                    CorrelationId = correlationId
                };

                action(apiCallContext);
            }
            catch (Exception ex)
            {
                traceLogger.LogError(ex.Message);
                throw;
            }
        }

        private static string GetLocalIpAddress()
        {
            var ipAddress = string.Empty;

            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    ipAddress = ip.ToString();
                    break;
                }
            }

            return ipAddress;
        }
    }
}