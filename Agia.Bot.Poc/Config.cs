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

    public string? FOUNDRY_PROJECT_ENDPOINT { get; set; }

    public string? FOUNDRY_PROJECT_KEY { get; set; }

    public string? FOUNDRY_AGENT_ID { get; set; }

    public string? FOUNDRY_AUTH_MODE { get; set; }
    public string? FOUNDRY_CLIENT_ID { get; set; }
    public string? FOUNDRY_CLIENT_SECRET { get; set; }

    public string? TENANT_ID { get; set; }

    public string? CLIENT_ID { get; set; }
}