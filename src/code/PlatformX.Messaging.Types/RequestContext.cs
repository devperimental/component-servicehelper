namespace PlatformX.Messaging.Types
{
    public class RequestContext
    {
        
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public string CorrelationId { get; set; }
        public string SessionId { get; set; }

        public string PortalName { get; set; }
        public string IdentityId { get; set; }
        public string OrganisationGlobalId { get; set; }
        public string UserGlobalId { get; set; }
        
        public string SystemApiRoleType { get; set; }

        public string ClientApplicationKey { get; set; }
        public string ClientApiKey { get; set; }
        public string ClientApplicationGlobalId { get; set; }
        public string ClientAppEnvironment { get; set; }

        public int ResponseCode { get; set; }
        public string ResponseContent { get; set; }
    }
}
