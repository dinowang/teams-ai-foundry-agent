using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Cards;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;

namespace Agia.Bot.Poc;

[TeamsController("main")]
public class MainController
{
    private readonly Config _config;
    private readonly ILogger _logger;

    public MainController(
        Config config,
        ILogger logger)
    {
        _config = config;
        _logger = logger;
    }

    [Message]
    public async Task OnMessage(
        IContext<MessageActivity> context,
        [Context] MessageActivity activity,
        [Context] IContext.Client client)
    {
        try
        {
            if (_config.FOUNDRY_AUTH_MODE == "JWT" && !context.IsSignedIn)
            {
                await context.SignIn();
                return;
            }

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                && !context.Storage.Exists("debug_show_jwt"))
            {
                await client.Send(new AdaptiveCard()
                {
                    Body = new List<CardElement>()
                    {
                        new TextBlock("Access Token")
                        {
                            Weight = TextWeight.Bolder,
                            Size = TextSize.Medium,
                            Wrap = true
                        },
                        new TextInput()
                        {
                            Id = "access_token",
                            Placeholder = "Access Token",
                            IsMultiline = true,
                            Height = ElementHeight.Stretch,
                            Value = context.UserGraphToken?.Token.RawData
                        },
                        new TextBlock("Only in development mode. Inspect with [jwt.ms](https://jwt.ms) ([Claims reference](https://learn.microsoft.com/en-us/entra/identity-platform/access-token-claims-reference)).")
                        {
                            Weight = TextWeight.Bolder,
                            Size = TextSize.Medium,
                            Wrap = true
                        }
                    }
                });

                context.Storage.Set("debug_show_jwt", true);
            }

            await client.Typing();

            var projectEndpointUri = new Uri(_config.FOUNDRY_PROJECT_ENDPOINT!);
            TokenCredential credential = _config.FOUNDRY_AUTH_MODE!.ToUpper() switch
            {
                // Delegated Permission with JWT token from Teams
                "JWT" => new AccessTokenCredential(context.UserGraphToken!),
                // Application Permission with Client ID and Client Secret
                "SP" => new ClientSecretCredential(_config.TENANT_ID, _config.FOUNDRY_CLIENT_ID, _config.FOUNDRY_CLIENT_SECRET),
                // Development with DefaultAzureCredential
                _ => new DefaultAzureCredential(new DefaultAzureCredentialOptions { TenantId = _config.MicrosoftTenantId }),
            };
            var projectClient = new AIProjectClient(projectEndpointUri, credential);


            var agentClient = projectClient.GetPersistentAgentsClient();
            var agent = await agentClient.Administration.GetAgentAsync(_config.FOUNDRY_AGENT_ID);

            var threadId = string.Empty;
            if (!context.Storage.Exists("foundry_thread_id"))
            {
                var thread = await agentClient.Threads.CreateThreadAsync();
                threadId = thread.Value.Id;
                _logger.LogInformation($"Create new thread id: {threadId}");
                await context.Storage.SetAsync("foundry_thread_id", threadId);
            }
            else
            {
                threadId = await context.Storage.GetAsync<string>("foundry_thread_id");
                _logger.LogInformation($"Continue with thread id: {threadId}");
            }

            var message = await agentClient.Messages.CreateMessageAsync(threadId, MessageRole.User, activity.Text, cancellationToken: context.CancellationToken);
            var run = await agentClient.Runs.CreateRunAsync(threadId, agent.Value.Id, cancellationToken: context.CancellationToken);

            while (run.Value.Status == RunStatus.Queued || run.Value.Status == RunStatus.InProgress)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100));
                run = await agentClient.Runs.GetRunAsync(threadId, run.Value.Id, cancellationToken: context.CancellationToken);
            }

            if (run.Value.Status != RunStatus.Completed)
            {
                _logger.LogWarning($"Run 未完成。狀態：{run.Value.Status}，錯誤：{run.Value.LastError?.Message}");
                return;
            }

            await foreach (var m in agentClient.Messages.GetMessagesAsync(threadId, cancellationToken: context.CancellationToken))
            {
                if (m.Role == MessageRole.Agent && m.ContentItems != null)
                {
                    foreach (var item in m.ContentItems.Take(1))
                    {
                        if (item is MessageTextContent mtc)
                        {
                            var msg = new MessageActivity()
                            {
                                Text = mtc.Text,
                            };
                            msg.AddAIGenerated();
                            await client.Send(msg);
                            break;
                        }
                    }

                    break;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            await client.Send(new AdaptiveCard()
            {
                Body = new List<CardElement>()
                {
                    new TextBlock("Error")
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Medium,
                        Color = TextColor.Attention,
                        Wrap = true,
                    },
                    new TextBlock(ex.Message)
                    {
                        Wrap = true,
                    },
                    new TextBlock(ex.StackTrace ?? string.Empty)
                    {
                        Wrap = true,
                    }
                }
            });
        }
    }
}
