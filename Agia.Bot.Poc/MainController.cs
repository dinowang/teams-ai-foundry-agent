using System.Text;
using System.Text.Json;
using Microsoft.Teams.Api.Activities;
using Microsoft.Teams.Apps;
using Microsoft.Teams.Apps.Annotations;
using Microsoft.Teams.Apps.Activities;
using Microsoft.Teams.Cards;
using Microsoft.Agents.AI;
using Azure.AI.Projects;
using Azure.AI.Agents.Persistent;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;
using Json.More;

namespace Agia.Bot.Poc;

[TeamsController("main")]
public class MainController
{
    private readonly Config _config;
    private readonly ILogger _logger;

    private readonly int _emitIntervalMs = 500;

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

            var debugKey = $"{context.Activity.Conversation.Id}.debug_show_jwt";

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development"
                && !context.Storage.Exists(debugKey))
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

                context.Storage.Set(debugKey, true);
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

            // await AgentFrameworkRespondAsync(context, activity, agentClient);
            await AiFoundryDirectRespondAsync(context, activity, agentClient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);

            await client.Send(new AdaptiveCard()
            {
                Body = new List<CardElement>()
                {
                    new TextBlock(ex.Message)
                    {
                        Weight = TextWeight.Bolder,
                        Size = TextSize.Medium,
                        Color = TextColor.Attention,
                        Wrap = true,
                    },
                    new TextBlock(ex.StackTrace ?? string.Empty)
                    {
                        Wrap = true,
                        Size = TextSize.Small,
                        FontType = FontType.Monospace
                    }
                }
            });
        }
    }


    private async Task<AgentThread> AgentFrameworkLoadOrCreateThreadAsync(IContext<MessageActivity> context, ChatClientAgent agent, string key)
    {
        if (context.Storage.Exists(key))
        {
            var savedJson = await context.Storage.GetAsync<string>(key);
            var saved = savedJson.AsJsonElement();

            return agent.DeserializeThread(saved, JsonSerializerOptions.Web); // resume
        }

        return agent.GetNewThread();
    }


    private async Task AgentFrameworkRespondAsync(IContext<MessageActivity> context, MessageActivity activity, PersistentAgentsClient agentClient)
    {
        var orchestratorAgent = await agentClient.GetAIAgentAsync(_config.FOUNDRY_AGENT_ID!);

        var saveKey = $"agent_thread_{activity.Conversation.Id}";
        var agentThread = await AgentFrameworkLoadOrCreateThreadAsync(context, orchestratorAgent, saveKey);

        var streamingUpdate = orchestratorAgent.RunStreamingAsync(activity.Text, agentThread, cancellationToken: context.CancellationToken);

        var completeMessage = new StringBuilder();
        var emitMessage = new StringBuilder();
        var lastFlush = DateTime.UtcNow;

        DateTime EmitUpdate()
        {
            if (emitMessage.Length > 0)
            {
                var m = emitMessage.ToString();
                emitMessage.Clear();
                completeMessage.Append(m);
                context.Stream.Emit(m);
            }
            return DateTime.UtcNow;
        }

        await foreach (var update in streamingUpdate.WithCancellation(context.CancellationToken))
        {
            if (!string.IsNullOrEmpty(update.Text))
            {
                emitMessage.Append(update.Text);

                if ((DateTime.UtcNow - lastFlush).TotalMilliseconds > _emitIntervalMs)
                    lastFlush = EmitUpdate();
            }
        }
        EmitUpdate();

        await context.Storage.SetAsync(saveKey, agentThread.Serialize(JsonSerializerOptions.Web).GetRawText());
    }

    private async Task AiFoundryDirectRespondAsync(IContext<MessageActivity> context, MessageActivity activity, PersistentAgentsClient agentClient)
    {
        var agent = await agentClient.Administration.GetAgentAsync(_config.FOUNDRY_AGENT_ID);
        var key = $"{context.Activity.Conversation.Id}.{context.Activity.From.Id}.foundry_thread_id";

        var threadId = string.Empty;
        if (!context.Storage.Exists(key))
        {
            var thread = await agentClient.Threads.CreateThreadAsync();
            threadId = thread.Value.Id;
            _logger.LogInformation($"Create new thread id: {threadId}");
            await context.Storage.SetAsync(key, threadId);
        }
        else
        {
            threadId = await context.Storage.GetAsync<string>(key);
            _logger.LogInformation($"Continue with thread id: {threadId}");
        }


        var message = await agentClient.Messages.CreateMessageAsync(threadId, MessageRole.User, activity.Text, cancellationToken: context.CancellationToken);
        var streamingUpdate = agentClient.Runs.CreateRunStreamingAsync(threadId, _config.FOUNDRY_AGENT_ID!, cancellationToken: context.CancellationToken);

        var completeMessage = new StringBuilder();
        var emitMessage = new StringBuilder();
        var lastFlush = DateTime.UtcNow;

        DateTime EmitUpdate()
        {
            if (emitMessage.Length > 0)
            {
                var m = emitMessage.ToString();
                emitMessage.Clear();
                completeMessage.Append(m);
                context.Stream.Emit(m);
            }
            return DateTime.UtcNow;
        }

        await foreach (var update in streamingUpdate.WithCancellation(context.CancellationToken))
        {
            if (update is MessageContentUpdate contentUpdate
                && !string.IsNullOrEmpty(contentUpdate.Text))
            {
                emitMessage.Append(contentUpdate.Text);

                if ((DateTime.UtcNow - lastFlush).TotalMilliseconds > _emitIntervalMs)
                    lastFlush = EmitUpdate();
            }
        }

        EmitUpdate();
    }
}
