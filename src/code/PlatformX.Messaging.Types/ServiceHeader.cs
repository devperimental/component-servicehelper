namespace PlatformX.Messaging.Types
{
    public class ServiceHeader
    {
        public string PortalName { get; set; }
        public string RequestServiceHash { get; set; }
        public string RequestServiceKey { get; set; }
        public string RequestServiceTimestamp { get; set; }
        public string IpAddress { get; set; }
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }
        public string IdentityId { get; set; }
        public string OrganisationGlobalId { get; set; }
        public string UserGlobalId { get; set; }
        public string SystemApiRoleType { get; set; }
        public string ClientApplicationKey { get; set; }
        public string ClientApiKey { get; set; }
        public string ClientApplicationGlobalId { get; set; }
        public string ClientAppEnvironment { get; set; }
    }
}
