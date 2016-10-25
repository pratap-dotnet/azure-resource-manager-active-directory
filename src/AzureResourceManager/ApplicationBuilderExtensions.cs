using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AzureResourceManager
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureAuthentication(this IApplicationBuilder app, 
            IConfigurationRoot configuration)
        {
            string clientId = configuration["AzureAD:ClientId"];
            string clientSecret = configuration["AzureAD:ClientSecret"];
            string authority = string.Format(configuration["AzureAD:Authority"], "common");
            string azureResourceManagerIdentifier = configuration["AzureAD:ResourceManagerIdentifier"];
            string redirectUri = configuration["AzureAD:RedirectUri"];

            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            app.UseMiddleware<MultiTenantOpenIdConnectMiddleware>(Options.Create(new OpenIdConnectOptions
            {
                ClientId = clientId,
                Authority = authority,
                ResponseType = OpenIdConnectResponseType.CodeIdToken,
                CallbackPath = @"/home/index",
                TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                {
                    ValidateIssuer = false
                },
                Events = new OpenIdConnectEvents
                {
                    OnRedirectToIdentityProvider = (context) =>
                    {
                        object obj = null;
                        if (string.IsNullOrEmpty(context.Options.Authority))
                        {
                            context.ProtocolMessage.IssuerAddress = authority;
                        }
                        context.ProtocolMessage.PostLogoutRedirectUri = redirectUri;
                        context.ProtocolMessage.Prompt = "select_account";
                        context.ProtocolMessage.Resource = azureResourceManagerIdentifier;
                        return Task.FromResult(0);
                    },
                    OnAuthorizationCodeReceived = async (context) =>
                    {
                        var credential = new ClientCredential(clientId, clientSecret);
                        string tenantId = context.Ticket.Principal.FindFirst("http://schemas.microsoft.com/identity/claims/tenantid").Value;

                        string signedInUserUniqueName = context.Ticket.Principal.FindFirst(ClaimTypes.Name).Value.Split('#')
                            [context.Ticket.Principal.FindFirst(ClaimTypes.Name).Value.Split('#').Length - 1];

                        var tokenCache = new TableTokenCache(signedInUserUniqueName, configuration["AzureAD:TokenStorageConnectionString"]);
                        tokenCache.Clear();

                        var request = context.HttpContext.Request;
                        var currentUri = UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, request.Path);

                        var authenticationContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", tenantId), tokenCache);
                        
                        var authResult = await authenticationContext.AcquireTokenByAuthorizationCodeAsync(context.TokenEndpointRequest.Code,
                            new Uri(currentUri), credential, azureResourceManagerIdentifier);
                        context.HandleCodeRedemption();
                    },
                    OnTokenValidated = (context) =>
                    {
                        string issuer = context.Ticket.Principal.FindFirst("iss").Value;
                        if (!issuer.StartsWith("https://sts.windows.net/"))
                        {
                            throw new System.IdentityModel.Tokens.SecurityTokenValidationException();
                        }
                        return Task.FromResult(0);
                    },
                    OnTokenResponseReceived = (context) =>
                    {
                        return Task.FromResult(0);
                    },
                    OnAuthenticationFailed = (context) =>
                    {
                        return Task.FromResult(0);
                    }
                }
            }));
        }
    }
}
