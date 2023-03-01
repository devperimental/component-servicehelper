using PlatformX.Messaging.Types;
using PlatformX.ServiceLayer.Types;

namespace PlatformX.ServiceLayer.Behaviours
{
    public interface IMicroServiceClientHelper
    {
        TResponse SubmitRequest<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, string actionName) where TResponse : GenericResponse;
    }
}
