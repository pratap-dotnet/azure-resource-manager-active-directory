# azure-resource-manager-active-directory
Azure Resource Manager and Azure Active Directory integration to manage 
customer resources using ASP.Net Core web application. 

# Note:
* This project is almost a migration from [Dushyanth's](https://github.com/dushyantgill)[CloudSense](http://github.com/dushyantgill/VipSwapper/tree/master/CloudSense) 
* More information about it in Microsoft documention [Resource Manager API Documentation](https://azure.microsoft.com/en-us/documentation/articles/resource-manager-api-authentication/)

# How to:
1. Create a Multi-tenant application in your Azure AD instance. 
2. Register the Reply address to \{baseUrl\}/home/index in Applications's Configure page.
3. Copy the ApplicationId and Generated secret from configure page to appsettings.json's ClientId and ClientSecret respectively.
4. Run the application.
