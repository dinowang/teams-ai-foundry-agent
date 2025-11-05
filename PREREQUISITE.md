# PREREQUISITE

## 前置需求

- Tools
  - Visual Studio Code  
    - Windows
      ```cmd
      winget install --id Microsoft.VisualStudioCode
      ```
    - macOS
      ```bash
      brew install visual-studio-code --cask
      ```
  - Visual Studio Code Extensions
    - Microsoft 365 Agents Toolkit 
      ```bash
      code --install-extension ms-azuretools.m365-agents-toolkit
      ```
    - Azure Resource 
      ```bash
      code --install-extension ms-vscode.vscode-node-azure-pack
      ```
    - Azure App Service
      ```bash
      code --install-extension ms-azuretools.vscode-azureappservice
      ```
  - Azure CLI  
    - Windows
      ```cmd
      winget install --id Microsoft.AzureCLI
      ```
    - macOS
      ```bash
      brew install azure-cli
      ```
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

