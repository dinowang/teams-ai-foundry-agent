public class Config
{
    public string? MicrosoftTenantId { get; set; }

    public string? MicrosoftAppId { get; set; }

    public string? MicrosoftAppPassword { get; set; }

    public string? MicrosoftAppType { get; set; }

    public string? AAD_APP_TENANT_ID { get; set; }

    public string? AAD_APP_CLIENT_ID { get; set; }

    public string? AAD_APP_OBJECT_ID { get; set; }

    public string? AAD_APP_CLIENT_SECRET { get; set; }

    public string? AAD_APP_OAUTH_AUTHORITY_HOST { get; set; }

    public string? AAD_APP_OAUTH_AUTHORITY { get; set; }

    public string? BOT_ID { get; set; }

    public string? BOT_PASSWORD { get; set; }

    public string? BOT_DOMAIN { get; set; }

    public string? BOT_TYPE { get; set; } //= "UserAssignedMsi";

    public string? BOT_OAUTH_PROFILE { get; set; } //= "graph";

    public string? FOUNDRY_PROJECT_ENDPOINT { get; set; } //= "https://aif-aiagent-d38344.services.ai.azure.com/api/projects/firstProject";

    public string? FOUNDRY_PROJECT_KEY { get; set; } //= "v66Wf9jJGSjtCiWuIFtvRr3lMGGI1BwvvgT3ahiRDQINwJ0b496EJQQJ99BHACHYHv6XJ3w3AAAAACOGcxuO";

    public string? FOUNDRY_AGENT_ID { get; set; } //= "asst_D8rIntVSjB0XELxLfwcyELfj";

    public string? FOUNDRY_AUTH_MODE { get; set; } //= "JWT";
    public string? FOUNDRY_CLIENT_ID { get; set; }
    public string? FOUNDRY_CLIENT_SECRET { get; set; }

    public string? TENANT_ID { get; set; }

    public string? CLIENT_ID { get; set; }
}