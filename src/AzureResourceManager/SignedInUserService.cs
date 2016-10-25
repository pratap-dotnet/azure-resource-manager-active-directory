using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace AzureResourceManager
{
    public class SignedInUserService
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public SignedInUserService(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }

        public string GetSignedInUserName()
        {
            var nameClaim = (httpContextAccessor.HttpContext.User.Identity as ClaimsIdentity).FindFirst(ClaimTypes.Name);
            return nameClaim.Value.Split('#')[nameClaim.Value.Split('#').Length - 1];
        }
    }
}
