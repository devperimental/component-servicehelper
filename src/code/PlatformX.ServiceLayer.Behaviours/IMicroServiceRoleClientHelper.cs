using PlatformX.Messaging.Types;
using PlatformX.ServiceLayer.Types;

namespace PlatformX.ServiceLayer.Behaviours
{
    public interface IMicroServiceRoleClientHelper
    {
        TResponse SubmitRequest<TRequest, TResponse>(TRequest request, RequestContext requestContext, ServiceMetaData serviceMetaData, string actionName, string envKey, string portNumber, string regionKey, string locationKey, string fulfilmentRoleType) where TResponse : GenericResponse;
    }
}