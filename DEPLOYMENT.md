# DEPLOYMENT

## 前置需求

- Tools
  - Visual Studio Code
  - Visual Studio Code Extensions
    - Microsoft 365 Agents Toolkit 
    - Azure Resource 
    - Azure App Service
  - Azure CLI (Bicep should be included)

- Settings
  - Microsoft Teams Admin Center ( https://admin.teams.microsoft.com/ )
    Enable Upload Custom Apps  
    https://learn.microsoft.com/en-us/microsoftteams/manage-apps#manage-org-wide-app-settings

- Azure 
  - App Service Plan + App Service (Windows, .NET 9)
    - Precompiled binary (provide later)
  - Azure Bot Service
    - User Assigned Managed Identity #1 for Bot itself
    - User Assigned Managed Identity #2 for SSO
  - Application Insights 

## 部署步驟

1. 使用 VSCode 打開this.code-workspace 檔 
   ```bash
   cd $PROJECT_FOLDER
   vscode this.code-workspace
   ```

2. 創建 Teams App 下的 `env/.env.dev` 設定專案屬性，以下為範例
   ```bash
   TEAMSFX_ENV=dev
   APP_NAME_SUFFIX=dev
   AZURE_SUBSCRIPTION_ID=447036d0-24d7-4fde-a61d-5e80272c10ee
   AZURE_RESOURCE_GROUP_NAME=rg-aiagent-d0928
   RESOURCE_SUFFIX=-d0928
   ```

3. Provision Teams app using Microsoft 365 Agents Toolkit
   - Sign in Microsoft 365
   - Sign in Azure 
   - Provision
     - Teams app manifest
     - Azure resources
     - Teams app manifest update
     - Teams app manifest update ( manual update in M365 Developer Portal https://dev.teams.microsoft.com/ )  
       ```json
       {
         ...
        "bots": [
          {
              "botId": "{bot id}",
              "scopes": [
                "personal",
                "team",
                "groupChat"
              ],
              "isNotificationOnly": false,
              "supportsCalling": false,
              "supportsVideo": false,
              "supportsFiles": false
            }
          ],
         "validDomains": [
           "{app service name}.azurewebsites.net",
           "*.devtunnels.ms",
           "*.botframework.com"
         ],
         "webApplicationInfo": {
           "id": "{bot id}",
           "resource": "api://botid-{bot id}"
         }
       }       
       ```

4. Service Principal
   創建或使用現有的 Service Principal (SP)，SP 應該被授予 AI Foundry 的 AI User 角色
   設定
   - Authentication

     Redirect URIs
     | URI                                                               |
     | ----------------------------------------------------------------- |
     | + https://token.botframework.com/.auth/web/redirect               |
     | + https://{your-app-service-name}.azurewebsites.net/auth-end.html |

   - API Permissions
     | API                             | Permissions                                                           |
     | ------------------------------- | --------------------------------------------------------------------- |
     | Azure Machine Learning Services | user_impersonation                                                    |
     | Microsoft Cognitive Services    | user_impersonation                                                    |
     | Microsoft Graph                 | email, offlice_access, openid, profile, User.Read, User.ReadBasic.All |

   - Expose an API
     | Field              | Value                                  |
     | ------------------ | -------------------------------------- |
     | Application ID URI | `api://botid-{Bot's Microsoft App ID}` |

     - Scopes defined by this API: 
       | Field            | Value                        |
       | ---------------- | ---------------------------- |
       | Scope name       | `access_as_user`             |
       | Who can consent? | `Admins and users` (by case) |
       | State            | `Enabled`                    |

     - Authorized client applications

       | Application                                                                          | Scope                                  |
       | ------------------------------------------------------------------------------------ | -------------------------------------- |
       | `5e3ce6c0-2b1f-4285-8d4b-75ee78787346` (This is Teams web application)               | `api://botid-{Bot's Microsoft App ID}` |
       | `1fec8e78-bce4-4aaf-ab1b-5451cc387264` (This is Teams mobile or desktop application) | `api://botid-{Bot's Microsoft App ID}` |

       > NOTE: https://learn.microsoft.com/en-us/microsoftteams/platform/bots/how-to/authentication/bot-sso-register-aad?tabs=botid 

   - Manifest
     ```json
     {
	     "id": "...",
	     "accessTokenAcceptedVersion": 2,
	     ...
     }
     ```

5. Bot Service 設定
   - OAuth profile

     | Field                     | Value                                                         |
     | ------------------------- | ------------------------------------------------------------- |
     | Name                      | `graph`                                                       |
     | Service Provider          | `AAD v2 with Federated Credentials`                           |
     | Client ID                 | `{Service Principal Client ID}`                               |
     | Unique Subject Identifier | `{guid}` (can be any random string)                           |
     | Token Exchange URL        | `api://botid-{Bot's Microsoft App ID}`                        |
     | Tenant ID                 | `{tenant-id}`                                                 |
     | Scopes                    | `openid profile offline_access https://ai.azure.com/.default` |
     
     > NOTE: https://ai.azure.com/.default is the Azure Machine Learning scope


   - Service Principal 
     - Certificates & secrets / Federated Credentials 
        
       | Field                         | Value                                                                                    |
       | ----------------------------- | ---------------------------------------------------------------------------------------- |
       | Federated Credential scenario | `Other issuer`                                                                           |
       | Issuer                        | `https://login.microsoftonline.com/{tenant-id}/v2.0`                                     |
       | Type                          | `Explicit subject identifier`                                                            |
       | Value                         | `/eid1/c/pub/t/{encoded-tenant-id}/a/9ExAW52n_ky4ZiS_jhpJIQ/{Unique Subject Identifier}` |

       > https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/azure-bot-user-authorization-federated-credentials
       >
       > Problem? check out: https://github.com/microsoft/Agents/issues/237

   - OAuth profile
     - Test connection

6. App Service 設定 
   - App Service Logs
   - Application Insights
   - Configurations
   - Environment Variables

7. Deploy bot application  
   Choose one of the following methods:
   - Zip Deploy (CLI)
   - Zip Deploy (Kudu)
   - Deploy from VSCode using M365 Agents Toolkit
