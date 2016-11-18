# Delegate Audit Log Exporter

## Installation

### Registration in Active Directory
First, register an App in the target tenant's Azure Active Directory.

You can follow a guide on how to do this via the Azure Portal [here](https://msdn.microsoft.com/en-us/office-365/get-started-with-office-365-management-apis). Make sure you do this from the Classic Portal, in order to get a Service Principal provisioned.

Take note of your App's ClientId.

You will also need to generate a key for the App (The ClientSecret), also depicted in the guide linked above. Make sure to copy the ClientSecret, because you will need it later and you cannot see it via the portal again after leaving the page.

The next step is to assign the necessary permissions to the App. This is also elaborated upon in the guide, under the heading, __Specify the permissions your app requires to access the Office 365 Management APIs__.

Finally, before leaving the portal, you should take note of the Tenant-Id for your Office 365 tenant, as you will need this for registering the WebHook later.

The tenant ID can be obtained from the URL when you are browsing the Active Directory blades in the Classic Portal. The URL will appear as below:

    https://manage.windowsazure.com/tenantname#Workspaces/ActiveDirectoryExtension/Directory/00000000-0000-0000-0000-000000000000/directoryQuickStart
    
That GUID is the Id for the Tenant.

### Setup DALE

After deploying DALE however you see fit, it is necessary to declare some environment variables. If you're hosting as an Azure Web App, you can easily set these environment variables in the Portal, under Application Settings.

    Tenant=yourdomain.onmicrosoft.com
    ClientId=yourclientId
    ClientSecret=yourclientSecret
    AzureConnectionString=yourStorageConnectionString
    
### Subscribe to WebHook

The last step is to configure your tenant's management API to send Audit events to DALE. You can do this via an HTTP POST to the management API.

A script is provided to automate this task. Begin by opening `subscribe.fsx` script and replacing the variables at the top of the file with the values you collected in the previous steps.

Then, you can execute the script via the `subscribe.cmd` or `subscribe.sh` wrappers.