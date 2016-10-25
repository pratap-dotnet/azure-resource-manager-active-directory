using System;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureResourceManager
{
    //Authentication middleware to create Authentication handler
    public class MultiTenantOpenIdConnectMiddleware : OpenIdConnectMiddleware
    {
        public MultiTenantOpenIdConnectMiddleware(
            RequestDelegate next, 
            IDataProtectionProvider dataProtectionProvider, 
            ILoggerFactory loggerFactory, 
            UrlEncoder encoder, 
            IServiceProvider services, 
            IOptions<SharedAuthenticationOptions> sharedOptions, 
            IOptions<OpenIdConnectOptions> options, HtmlEncoder htmlEncoder) 
            : base(next, dataProtectionProvider, loggerFactory, encoder, services, sharedOptions, options, htmlEncoder)
        {
            
        }

        protected override AuthenticationHandler<OpenIdConnectOptions> CreateHandler()
        {
            return new MultiTenantOpenIdConnectHandler(Backchannel, HtmlEncoder);
        }
    }
}
