# DEPLOYMENT

user2@MngEnvMCAP247195.onmicrosoft.com


## 需求條件

- Tools
  - Visual Studio Code
  - Visual Studio Code Extensions
    - Microsoft 365 Agents Toolkit 
    - Azure Resource 
    - Azure App Service
  - Azure CLI (Bicep should be included)

- Settings
  - Microsoft Teams Admin Center  
    Enable Upload Custom Apps  
    https://learn.microsoft.com/en-us/microsoftteams/manage-apps#manage-org-wide-app-settings

- Azure 
  - App Service Plan + App Service (Windows, .NET 9)
    - Precompiled binary (provide later)
  - Azure Bot Service
    - User Assigned Managed Identity #1 for Bot itself
    - User Assigned Managed Identity #2 for SSO
  - Application Insights (optional)

## 部署步驟

1. 使用 VSCode 打開this.code-workspace 檔 
   ```bash
   cd $PROJECT_FOLDER
   vscode this.code-workspace
   ```

2. 編輯 Teams App 下的 `env/.env.dev` 設定專案屬性，以下為起始範例
   ```bash
   TEAMSFX_ENV=dev
   APP_NAME_SUFFIX=dev
   AZURE_SUBSCRIPTION_ID=447036d0-24d7-4fde-a61d-5e80272c10ee
   AZURE_RESOURCE_GROUP_NAME=rg-aiagent-d0928
   RESOURCE_SUFFIX=-d0928
   ```

3. VSCode provision
   - Teams app manifest
   - Azure resources
   - Teams app manifest update
   - Teams app manifest update (manual update)

4. Bot Service 設定
   - OAuth profile
     - Name: `graph`
     - Service Provider: `AAD v2 with Federated Credentials`
     - Client ID: `{Service Principal Client ID}`
     - Unique Subject Identifier: `{guid}` (can be any random string)
     - Token Exchange URL: `api://botid-{Bot's Microsoft App ID}`
     - Tenant ID: `{tenant-id}`
     - Scopes: `openid profile offline_access https://ai.azure.com/.default`
       NOTE: https://ai.azure.com/.default actually is Azure Machine Learning scope

   - Service Principal 
     - federated credential
       - Federated credential scenario: `Other issuer`
       - Issuer: `https://login.microsoftonline.com/{tenant-id}/v2.0`
       - Type: `Explicit subject identifier`
       - Value: `/eid1/c/pub/t/{encoded-tenant-id}/a/9ExAW52n_ky4ZiS_jhpJIQ/{Unique Subject Identifier}`  
         Reference: https://github.com/microsoft/Agents/issues/237

     - API Permissions
       1. Azure Machine Learning Services
          - user_impersonation
       2. Microsoft Cognitive Services
          - user_impersonation
       3. Microsoft Graph
          - email
          - offlice_access
          - openid
          - profile
          - User.Read
          - User.ReadBasic.All
   - Test connection

5. App Service 設定 
   - App Service Logs
   - Environment Variables

6. VSCode deploy


7. Service Principal's Manifest
   ```json
   {
	   "id": "...",
	   "accessTokenAcceptedVersion": 2,
	   ...
   }
   ```