using System.Collections.Generic;
using System.Threading.Tasks;

namespace PlatformX.Queues.Behaviours
{
    public interface IQueueXClient
    {
        Task SendMessage(string data, Dictionary<string, string> headers, string messageId, string roleKey, string regionKey, string locationKey, string queueName);

        Task SendMessage(string data, Dictionary<string, string> headers, string messageId, string roleKey, string regionKey, string locationKey, string queueName, int deferSeconds);
        
        Task SendClientMessage(string data, Dictionary<string, string> headers, string messageId, string queueName,
            string regionKey, string locationKey);

        Task SendManagementMessage(string data, Dictionary<string, string> headers, string messageId, string queueName,
            string regionKey, string locationKey);
    }
}