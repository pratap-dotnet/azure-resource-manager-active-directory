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
        private readonly ISubscriptionRepository subscriptionRepository;
        private readonly SignedInUserService signedInUserService;

        public HomeController(IOptions<AzureADSettings> settings, AzureResourceManagerUtil resourceManagerUtilty,
            ISubscriptionRepository subscriptionRepository, SignedInUserService signedInUserService)
        {
            azureADSettings = settings.Value;
            this.resourceManagerUtility = resourceManagerUtilty;
            this.subscriptionRepository = subscriptionRepository;
            this.signedInUserService = signedInUserService;
        }

        public async Task<IActionResult> Index()
        {
            ViewModel model = null;
            if (User.Identity.IsAuthenticated)
            {
                string userId = signedInUserService.GetSignedInUserName();
                model = new ViewModel();
                model.ConnectedSubscriptions = subscriptionRepository.GetAllSubscriptionsForUser(userId).ToList();
                foreach (var connectedSubscription in model.ConnectedSubscriptions)
                {
                    bool servicePrincipalHasReadAccessToSubscription = await resourceManagerUtility.
                        DoesServicePrincipalHaveReadAccessToSubscription(connectedSubscription.Id, connectedSubscription.DirectoryId);
                    connectedSubscription.AzureAccessNeedsToBeRepaired = !servicePrincipalHasReadAccessToSubscription;
                }
            }

            return View(model);
        }

        public async Task ConnectSubscription(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            if (!string.IsNullOrEmpty(directoryId))
            {
                if (!User.Identity.IsAuthenticated || !directoryId.Equals((User.Identity as ClaimsIdentity).FindFirst
                    ("http://schemas.microsoft.com/identity/claims/tenantid").Value))
                {
                    //This is where the actual magic of changing authentication authority happens
                    var openIdFeature = HttpContext.Features[typeof(IHttpAuthenticationFeature)] as IHttpAuthenticationFeature;
                    var openIdHandler = openIdFeature.Handler as MultiTenantOpenIdConnectHandler;
                    openIdHandler.SetTenantAuthority(string.Format(azureADSettings.Authority, directoryId));
                    
                    Dictionary<string, string> dict = new Dictionary<string, string>();
                    dict["prompt"] = "select_account";

                    await HttpContext.Authentication.ChallengeAsync(OpenIdConnectDefaults.AuthenticationScheme,
                        new AuthenticationProperties(dict) { RedirectUri = this.Url.Action("ConnectSubscription", "Home") + "?subscriptionId=" + subscriptionId });
                }
                else
                {
                    string objectIdOfCloudSenseServicePrincipalInDirectory = await
                        resourceManagerUtility.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

                    await resourceManagerUtility.GrantRoleToServicePrincipalOnSubscription
                        (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

                    Subscription s = new Subscription()
                    {
                        Id = subscriptionId,
                        DirectoryId = directoryId,
                        ConnectedBy = signedInUserService.GetSignedInUserName(),
                        ConnectedOn = DateTime.Now
                    };

                    subscriptionRepository.AddSubscription(s);
                    Response.Redirect(this.Url.Action("Index", "Home"));
                }
            }

            return;
        }
        public async Task DisconnectSubscription(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            string objectIdOfCloudSenseServicePrincipalInDirectory = await
                resourceManagerUtility.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

            await resourceManagerUtility.RevokeRoleFromServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

            Subscription s = subscriptionRepository.GetByUserAndId(signedInUserService.GetSignedInUserName(), subscriptionId);
            if (s != null)
            {
                subscriptionRepository.Remove(s);
            }

            Response.Redirect(this.Url.Action("Index", "Home"));
        }
        public async Task RepairSubscriptionConnection(string subscriptionId)
        {
            string directoryId = await resourceManagerUtility.GetDirectoryForSubscription(subscriptionId);

            string objectIdOfCloudSenseServicePrincipalInDirectory = await
                resourceManagerUtility.GetObjectIdOfServicePrincipalInDirectory(directoryId, azureADSettings.ClientId);

            await resourceManagerUtility.RevokeRoleFromServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);
            await resourceManagerUtility.GrantRoleToServicePrincipalOnSubscription
                (objectIdOfCloudSenseServicePrincipalInDirectory, subscriptionId, directoryId);

            Response.Redirect(this.Url.Action("Index", "Home"));
        }
        
    }
}
