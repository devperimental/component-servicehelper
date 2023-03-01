using Microsoft.Azure.ServiceBus.Primitives;
using Microsoft.Azure.Services.AppAuthentication;
using System;
using System.Threading.Tasks;

namespace PlatformX.Queues.ServiceBus
{
    public class ServiceBusTokenProvider : TokenProvider
    {
        private readonly string _managedIdentityTenantId;

        public ServiceBusTokenProvider(string managedIdentityTenantId)
        {
            _managedIdentityTenantId = managedIdentityTenantId;
        }

        public override async Task<SecurityToken> GetTokenAsync(string appliesTo, TimeSpan timeout)
        {
            string accessToken = await GetAccessToken("https://servicebus.azure.net/");
            return new JsonSecurityToken(accessToken, appliesTo);
        }

        private async Task<string> GetAccessToken(string resource)
        {
            var authProvider = new AzureServiceTokenProvider();
            string tenantId = _managedIdentityTenantId;

            if (tenantId != null && tenantId.Length == 0)
            {
                tenantId = null;
            }
            return await authProvider.GetAccessTokenAsync(resource, tenantId);
        }
    }
}
