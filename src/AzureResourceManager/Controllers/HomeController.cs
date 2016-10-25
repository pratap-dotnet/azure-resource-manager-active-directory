using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AzureResourceManager.Models;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Http.Features.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace AzureResourceManager.Controllers
{
    public class HomeController : Controller
    {
        private readonly AzureADSettings azureADSettings;
        private readonly AzureResourceManagerUtil resourceManagerUtility;

        public HomeController(IOptions<AzureADSettings> settings, AzureResourceManagerUtil resourceManagerUtilty)
        {
            azureADSettings = settings.Value;
            this.resourceManagerUtility = resourceManagerUtilty;
        }

        public async Task<IActionResult> Index()
        {
            ViewModel model = null;
            if (ClaimsPrincipal.Current.Identity.IsAuthenticated)
            {
                string userId = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value;
                model = new ViewModel();
                model.ConnectedSubscriptions = new List<Subscription>();
                
            }

            return View(model);
        }

        public async Task ConnectSubscription(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            if (!String.IsNullOrEmpty(directoryId))
            {
                if (!User.Identity.IsAuthenticated || !directoryId.Equals((User.Identity as ClaimsIdentity).FindFirst
                    ("http://schemas.microsoft.com/identity/claims/tenantid").Value))
                {
                    var openIdFeature = HttpContext.Features[typeof(IHttpAuthenticationFeature)] as IHttpAuthenticationFeature;
                    var openIdHandler = openIdFeature.Handler as MultiTenantOpenIdConnectHandler;
                    openIdHandler.SetTenantAuthority(string.Format(azureADSettings.Authority, directoryId));
                    
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict["Authority"] = string.Format(azureADSettings.Authority + "OAuth2/Authorize", directoryId);
                    dict["prompt"] = "select_account";

                    await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                        new AuthenticationProperties(dict) { RedirectUri = this.Url.Action("ConnectSubscription", "Home") + "?subscriptionId=" + subscriptionId });
                }
                else
                {
                    string objectIdOfCloudSenseServicePrincipalInDirectory = await
                        AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

                    await resourceManagerUtility.GrantRoleToServicePrincipalOnSubscription
                        (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

                    Subscription s = new Subscription()
                    {
                        Id = subscriptionId,
                        DirectoryId = directoryId,
                        ConnectedBy = ClaimsPrincipal.Current.FindFirst(ClaimTypes.Name).Value,
                        ConnectedOn = DateTime.Now
                    };

                    //if (db.Subscriptions.Find(s.Id) == null)
                    //{
                    //    db.Subscriptions.Add(s);
                    //    db.SaveChanges();
                    //}

                    Response.Redirect(this.Url.Action("Index", "Home"));
                }
            }

            return;
        }
        public async Task DisconnectSubscription(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            string objectIdOfCloudSenseServicePrincipalInDirectory = await
                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

            await resourceManagerUtility.RevokeRoleFromServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

            //Subscription s = db.Subscriptions.Find(subscriptionId);
            //if (s != null)
            //{
            //    db.Subscriptions.Remove(s);
            //    db.SaveChanges();
            //}

            Response.Redirect(this.Url.Action("Index", "Home"));
        }
        public async Task RepairSubscriptionConnection(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            string objectIdOfCloudSenseServicePrincipalInDirectory = await
                AzureADGraphAPIUtil.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

            await resourceManagerUtility.RevokeRoleFromServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);
            await resourceManagerUtility.GrantRoleToServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

            Response.Redirect(this.Url.Action("Index", "Home"));
        }
    }
}
