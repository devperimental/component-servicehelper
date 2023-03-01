namespace PlatformX.Queues.Types
{
    public class BusDefinition
    {
        public string ServiceBusNamespace { get; set; }
        public bool LoggingEnabled { get; set; }
        public string TenantId { get; set; }
    }
}
