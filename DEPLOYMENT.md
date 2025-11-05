# DEPLOYMENT

## 相關連結

- [M365 Admin Portal](https://admin.microsoft.com)
- [Entra ID Portal](https://entra.microsoft.com)
- [Teams Admin Portal](https://admin.teams.microsoft.com)
- [Teams Developer Portal](https://dev.teams.microsoft.com)
- [Azure Portal](https://portal.azure.com)

## 前置需求

- Tools
  - Visual Studio Code
  - Visual Studio Code Extensions
    - Microsoft 365 Agents Toolkit 
    - Azure Resource 
    - Azure App Service
  - Azure CLI
  - Bicep CLI
    ```bash
    az bicep install
    ```

- Settings
  - 在 Microsoft Teams Admin Center 中 Enable Upload Custom Apps  
    https://learn.microsoft.com/en-us/microsoftteams/manage-apps#manage-org-wide-app-settings

- Azure 
  - App Service Plan + App Service (Windows, .NET 9)
    - Precompiled binary (provide later)
  - Azure Bot Service
    - User Assigned Managed Identity 
  - Application Insights  

## 部署步驟

1. 使用 VSCode 打開this.code-workspace 檔 
   ```bash
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
   - 打開 Microsoft 365 Agents Toolkit extension
     - 登入 Microsoft 365
     - 登入 Azure
     - 在環境中選擇 dev
     - 執行 Provision
       - (自動化) 生成/更新 Teams app 與 manifest
       - (自動化) 部署 Azure resources
       - (自動化) 新增/更新 Service Principal
       - (自動化) 更新 Teams app manifest 
     - 完成 Provision 後
       - 檢查在 Teams app 的 build 目錄中產生 app.manifest.dev.json 檔案
       - 檢查在 Teams app 的 env 目錄的 .env.dev 是否附加了物件參數
         ```bash
         TEAMS_APP_TENANT_ID=...
         TEAMS_APP_ID=...
         BOT_ID=...
         BOT_AZURE_APP_SERVICE_RESOURCE_ID=/subscriptions/.../resourceGroups/.../providers/Microsoft.Web/sites/...
         BOT_DOMAIN=.......azurewebsites.net
         BOT_TENANT_ID=...
         AAD_APP_CLIENT_ID=...
         AAD_APP_OBJECT_ID=...
         AAD_APP_TENANT_ID=...
         AAD_APP_OAUTH_AUTHORITY=https://login.microsoftonline.com/...
         AAD_APP_OAUTH_AUTHORITY_HOST=https://login.microsoftonline.com
         AAD_APP_ACCESS_AS_USER_PERMISSION_ID=...
         AAD_APP_CLIENT_SECRET=...
         ```
       - 在 Teams Developer Portal 中，應該能找到上傳的 Teams app  
         > 如果 Provision 順利結束，卻遲遲未出現在 Teams Developer Portal 中，請利用 Apps 頁面中的 `Take Ownership` 輸入 `.env.dev` 中的 `TEAMS_APP_ID` 值並尋找和取得該 App 的擁有權

4. Service Principal
   使用 .env.dev 中的 AAD_APP_CLIENT_ID 值在 Entra ID 找到 Service Principal (SP)
   
   此 SP 應該被授予 AI Foundry 的 AI User 角色

   設定(檢查)
   - Authentication

     Redirect URIs
     | URI                                                             |
     | --------------------------------------------------------------- |
     | https://token.botframework.com/.auth/web/redirect               |
     | https://{your-app-service-name}.azurewebsites.net/auth-end.html |

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
   - 於 Configuration 中，增加一組 OAuth profile

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

     完成後先存檔再重新打開 OAuth profile 編輯頁面

   - 打開先前設定的 Service Principal
     - 在 Certificates & secrets 中，新增一組 Federated Credential
        
       | Field                         | Value                                                                                        |
       | ----------------------------- | -------------------------------------------------------------------------------------------- |
       | Federated Credential scenario | `Other issuer`                                                                               |
       | Issuer                        | `https://login.microsoftonline.com/{tenant-id}/v2.0`                                         |
       | Type                          | `Explicit subject identifier`                                                                |
       | Value                         | `/eid1/c/pub/t/{encoded-tenant-id}/a/{base64-encoded-client-id}/{Unique Subject Identifier}` |

       > https://learn.microsoft.com/en-us/microsoft-365/agents-sdk/azure-bot-user-authorization-federated-credentials
       >
       > Problem? check out: https://github.com/microsoft/Agents/issues/237

   - OAuth profile
     - Test connection

6. App Service 設定 
   - App Service Logs
     - 打開 Application Logging (Filesystem)
   - Application Insights
     - 新建 Application Insights 實例，綁定至 App Service
   - Configurations
     - Stack: `.NET 9.0`
   - Environment Variables

7. Deploy bot application  
   Choose one of the following methods:
   - Zip Deploy (CLI)
   - Zip Deploy (Kudu)
   - Deploy from VSCode using M365 Agents Toolkit
