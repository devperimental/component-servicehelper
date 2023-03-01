using System;

namespace PlatformX.Queues.Types
{
    public class QueueDefinition
    {
        public string QueueName { get; set; }
        public TimeSpan MessageTimeToLive { get; set; }
        public bool EnableDeadLettering { get; set; }
        public TimeSpan LockDuration { get; set; }
        public int MaxDeliveryCount { get; set; }
        public bool IsDeadLetterQueue { get; set; }
    }
}
