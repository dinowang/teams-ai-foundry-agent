using Microsoft.Teams.Apps;
using Microsoft.Teams.Plugins.AspNetCore.Extensions;
using Microsoft.Teams.Common.Http;
using Microsoft.Teams.Api.Auth;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Plugins.AspNetCore.DevTools.Extensions;
using Azure.AI.Projects;
using Azure.Core;
using Azure.Identity;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.AspNetCore.Http.Metadata;
using Agia.Bot.Poc;

var logger = LoggerFactory.Create(logging =>
{
    logging.AddConsole();
}).CreateLogger("Program");

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenTelemetry().UseAzureMonitor();
///builder.AddTeams();

var appBuilder = App.Builder();

var userAssignedMsiId = Environment.GetEnvironmentVariable("CLIENT_ID");
logger.LogInformation($"UserAssignedMsiId: {userAssignedMsiId}");

// Obtain token from Managed Identity
Func<string?, string[], Task<ITokenResponse>> createTokenFactory = async (string? tenantId, string[] scopes) =>
{
    var managedIdentityCredential = new ManagedIdentityCredential(userAssignedMsiId);
    var tokenRequestContext = new TokenRequestContext(scopes, tenantId: tenantId);
    var accessToken = await managedIdentityCredential.GetTokenAsync(tokenRequestContext);

    logger.LogInformation($"Get access token for scopes: {string.Join(",", scopes)}, tenantId: {tenantId}, token: {accessToken.Token.Substring(0, 10)}...");

    return new TokenResponse
    {
        TokenType = "Bearer",
        AccessToken = accessToken.Token,
    };
};

// Offer credential for Bot Service 
appBuilder.AddCredentials(new TokenCredentials(
        userAssignedMsiId!,
        async (tenantId, scopes) =>
        {
            logger.LogInformation($"createTokenFactory: userAssignedMsiId={userAssignedMsiId}, tenantId={tenantId}, scopes={string.Join(",", scopes)}");
            return await createTokenFactory(tenantId, scopes);
        }));

var config = builder.Configuration.Get<Config>()!;
logger.LogInformation($"Config: {System.Text.Json.JsonSerializer.Serialize(config)}");

if (!string.IsNullOrEmpty(config.BOT_OAUTH_PROFILE))
{
    appBuilder.AddOAuth(config.BOT_OAUTH_PROFILE);
}

builder.AddTeams(appBuilder);

if (builder.Environment.IsDevelopment())
{
    builder.AddTeamsDevTools();
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<MainController>();
builder.Services.AddTransient(x => config);

var app = builder.Build();
app.UseStaticFiles();

app.MapGet("/", () =>
{
    logger.LogInformation("Health check endpoint called.");

    if (app.Environment.IsDevelopment())
    {
        return "Service is running (Development)...";
    }
    return "Service is running ...";
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.MapGet("/debug/env", (IConfiguration configuration) =>
    {
        var data = configuration.AsEnumerable().OrderBy(x => x.Key);
        var firstColumn = data.Select(x => x.Key).Max(x => x.Length) + 1;

        return string.Join('\n', data.Select(x => $"{x.Key.PadRight(firstColumn)} = {x.Value}"));
    });

    app.MapGet("/debug/config", () =>
    {
        var data = config.GetType().GetProperties().OrderBy(x => x.Name);
        var firstColumn = data.Select(x => x.Name).Max(x => x.Length) + 1;

        return string.Join('\n', data.Select(x => $"{x.Name.PadRight(firstColumn)} = {x.GetValue(config)}"));
    });

    app.MapGet("/debug/routes", (IHttpContextAccessor httpContextAccessor, IEnumerable<EndpointDataSource> endpointSources) =>
    {
        var httpContext = httpContextAccessor.HttpContext!;
        var baseUri = new Uri($"{httpContext.Request.Scheme}://{httpContext.Request.Host}");

        var data = endpointSources.SelectMany(source => source.Endpoints).Select(x => new
        {
            Path = new Uri(baseUri, x.Metadata.OfType<IRouteDiagnosticsMetadata>().FirstOrDefault()?.Route!).ToString(),
            Methods = string.Join('/', x.Metadata.OfType<HttpMethodMetadata>().FirstOrDefault()?.HttpMethods!)
        });

        return string.Join("\n", data.Select(x => $"{x.Methods} {x.Path}"));
    });
}

app.UseTeams();

app.Run();

