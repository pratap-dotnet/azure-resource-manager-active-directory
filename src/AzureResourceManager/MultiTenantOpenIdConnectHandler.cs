using System;
using System.Net.Http;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureResourceManager
{
    //OpenId Connect handler that allows Controller to access and modify tenants
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
            //Configuration manager would have login.microsoftonline.com/common as the endpoint provider, below line of code would refresh the token endpoints with new latest authority
            Options.ConfigurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(Options.MetadataAddress, new OpenIdConnectConfigurationRetriever(),
                        new HttpDocumentRetriever(Backchannel) { RequireHttps = Options.RequireHttpsMetadata });
        }
    }
}
