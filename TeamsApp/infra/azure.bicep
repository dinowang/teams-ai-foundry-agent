@maxLength(20)
@minLength(4)
@description('Used to generate names for all resources in this file')
param resourceBaseName string

param webAppSKU string

@maxLength(42)
param botDisplayName string

param serverfarmsName string = resourceBaseName
param webAppName string = resourceBaseName
param identityName string = resourceBaseName
param location string = resourceGroup().location

resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  location: location
  name: identityName
}

// Compute resources for your Web App
resource serverfarm 'Microsoft.Web/serverfarms@2021-02-01' = {
  kind: 'app'
  location: location
  name: serverfarmsName
  sku: {
    name: webAppSKU
  }
}

// Web App that hosts your agent
resource webApp 'Microsoft.Web/sites@2021-02-01' = {
  kind: 'app'
  location: location
  name: webAppName
  properties: {
    serverFarmId: serverfarm.id
    httpsOnly: true
    siteConfig: {
      alwaysOn: true
      appSettings: [
        {
          name: 'ASPNETCORE_ENVIRONMENT'
          value: 'Production'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: ''
        }
        {
          name: 'ApplicationInsightsAgent_EXTENSION_VERSION'
          value: '~2'
        }
        {
          name: 'XDT_MicrosoftApplicationInsights_Mode'
          value: 'default'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1' // Run Azure APP Service from a package file
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~20' // Set NodeJS version to 20.x for your site
        }
        {
          name: 'RUNNING_ON_AZURE'
          value: '1'
        }
        {
          name: 'TENANT_ID'
          value: identity.properties.tenantId
        }
        {
          name: 'CLIENT_ID'
          value: identity.properties.clientId
        }
        {
          name: 'BOT_TENANT_ID'
          value: identity.properties.tenantId
        }
        {
          name: 'BOT_DOMAIN'
          value: ''
        }
        {
          name: 'BOT_TYPE'
          value: 'UserAssignedMsi'
        }
        {
          name: 'BOT_OAUTH_PROFILE'
          value: 'graph'
        }
        {
          name: 'AAD_APP_TENANT_ID'
          value: identity.properties.tenantId
        }
        {
          name: 'AAD_APP_CLIENT_ID'
          value: ''
        }
        {
          name: 'AAD_APP_OBJECT_ID'
          value: ''
        }
        {
          name: 'AAD_APP_OAUTH_AUTHORITY'
          value: ''
        }
        {
          name: 'AAD_APP_OAUTH_AUTHORITY_HOST'
          value: ''
        }
        {
          name: 'AAD_APP_ACCESS_AS_USER_PERMISSION_ID'
          value: ''
        }
        {
          name: 'FOUNDRY_AGENT_ID'
          value: ''
        }
        {
          name: 'FOUNDRY_PROJECT_ENDPOINT'
          value: ''
        }
        {
          name: 'FOUNDRY_AUTH_MODE'
          value: 'JWT'
        }
        {
          name: 'FOUNDRY_CLIENT_ID'
          value: ''
        }
        {
          name: 'FOUNDRY_CLIENT_SECRET'
          value: ''
        }
        {
          name: 'MicrosoftAppId'
          value: identity.properties.clientId
        }
        {
          name: 'MicrosoftAppPassword'
          value: ''
        }
        {
          name: 'MicrosoftAppType'
          value: 'UserAssignedMSI'
        }
        {
          name: 'MicrosoftTenantId'
          value: identity.properties.tenantId
        }
      ]
      ftpsState: 'FtpsOnly'
    }
  }
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${identity.id}': {}
    }
  }
}

// Register your web service as a bot with the Bot Framework
module azureBotRegistration './botRegistration/azurebot.bicep' = {
  name: 'Azure-Bot-registration'
  params: {
    resourceBaseName: resourceBaseName
    identityClientId: identity.properties.clientId
    identityResourceId: identity.id
    identityTenantId: identity.properties.tenantId
    botAppDomain: webApp.properties.defaultHostName
    botDisplayName: botDisplayName
  }
}

// The output will be persisted in .env.{envName}. Visit https://aka.ms/teamsfx-actions/arm-deploy for more details.
output BOT_AZURE_APP_SERVICE_RESOURCE_ID string = webApp.id
output BOT_DOMAIN string = webApp.properties.defaultHostName
output BOT_ID string = identity.properties.clientId
output BOT_TENANT_ID string = identity.properties.tenantId
