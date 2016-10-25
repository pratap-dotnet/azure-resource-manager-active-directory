namespace AzureResourceManager.Models
{
    public class AzureADSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string RedirectUri { get; set; }
        public string Authority { get; set; }
        public string ResourceManagerIdentifier { get; set; }
        public string ARMAuthorizationPermissionsAPIVersion { get; set; }
        public string ResourceManagerUrl { get;  set; }
        public string RequiredARMRoleOnSubscription { get;  set; }
        public string ARMAuthorizationRoleAssignmentsAPIVersion { get;  set; }
        public string ARMAuthorizationRoleDefinitionsAPIVersion { get;  set; }
        public string TokenStorageConnectionString { get;  set; }
    }
}
