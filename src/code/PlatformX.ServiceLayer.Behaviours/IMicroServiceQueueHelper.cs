using PlatformX.Messaging.Types;
using PlatformX.ServiceLayer.Types;
using System.Threading.Tasks;

namespace PlatformX.ServiceLayer.Behaviours
{
    public interface IMicroServiceQueueHelper
    {
        Task<TResponse> SubmitQueueMessage<TRequest, TResponse>(TRequest request,
            RequestContext requestContext,
            ServiceMetaData serviceMetaData,
            string queueName,
            string systemApiRoleType = "",
            string regionKey = "",
            string locationKey = "") where TResponse : GenericResponse, new();
    }
}
