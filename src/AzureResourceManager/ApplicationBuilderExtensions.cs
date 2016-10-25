using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace AzureResourceManager
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureAuthentication(this IApplicationBuilder app, IConfigurationRoot configuration)
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

                        var authenticationContext = new AuthenticationContext(string.Format("https://login.windows.net/{0}", tenantId));
                        var authResult = await authenticationContext.AcquireTokenByAuthorizationCodeAsync(context.JwtSecurityToken.RawData,
                            new Uri(redirectUri), credential);
                    },
                    OnTokenValidated = (context) =>
                    {
                        string issuer = context.Ticket.Principal.FindFirst("iss").Value;
                        if (!issuer.StartsWith("https://sts.windows.net/"))
                        {
                            throw new System.IdentityModel.Tokens.SecurityTokenValidationException();
                        }
                        return Task.FromResult(0);
                    }
                }
            }));
        }
    }
}
