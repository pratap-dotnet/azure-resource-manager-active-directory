using System;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureResourceManager
{
    public class MultiTenantOpenIdConnectHandler : OpenIdConnectHandler
    {
        public MultiTenantOpenIdConnectHandler(HttpClient backchannel, HtmlEncoder htmlEncoder) : base(backchannel, htmlEncoder)
        {
        }
        
        public void SetTenantAuthority(string authority)
        {
            this.Options.Authority = authority;
            Options.MetadataAddress = Options.Authority;
            if (!Options.MetadataAddress.EndsWith("/", StringComparison.Ordinal))
            {
                Options.MetadataAddress += "/";
            }
            Options.MetadataAddress += ".well-known/openid-configuration";
            Options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Options.MetadataAddress, new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(Backchannel) { RequireHttps = Options.RequireHttpsMetadata });
        }
    }
}
