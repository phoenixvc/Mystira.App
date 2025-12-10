# Microsoft Teams Bot Webhook Setup Guide

## Overview

This guide covers setting up webhooks for the Microsoft Teams bot integration in Mystira.App. The Teams bot uses the Microsoft Bot Framework to receive and process messages.

## Prerequisites

- Azure subscription
- Azure Bot registration
- Microsoft 365 tenant (for Teams deployment)
- Valid App ID and App Password from Azure AD

## Configuration Steps

### 1. Create Azure Bot Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Create a new **Azure Bot** resource
3. Choose **Multi-tenant** for the Bot type
4. Note down:
   - Microsoft App ID
   - Microsoft App Password (create new if needed)

### 2. Configure Messaging Endpoint

Set your messaging endpoint in Azure Bot settings:

```
https://your-domain.com/api/messages/teams
```

This endpoint will receive all incoming messages and activities from Teams.

### 3. Update appsettings.json

```json
{
  "Teams": {
    "Enabled": true,
    "MicrosoftAppId": "<your-app-id>",
    "MicrosoftAppPassword": "<your-app-password>",
    "TenantId": "<optional-tenant-id>",
    "DefaultTimeoutSeconds": 30,
    "EnableAdaptiveCards": true
  }
}
```

### 4. Create Webhook Controller

Create a controller to handle incoming Teams messages:

```csharp
[ApiController]
[Route("api/messages")]
public class TeamsWebhookController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;

    public TeamsWebhookController(IBotFrameworkHttpAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [HttpPost("teams")]
    public async Task PostAsync()
    {
        await _adapter.ProcessAsync(Request, Response, _bot);
    }
}
```

### 5. Implement Bot Handler

```csharp
public class TeamsBotHandler : ActivityHandler
{
    private readonly TeamsBotService _botService;

    public TeamsBotHandler(TeamsBotService botService)
    {
        _botService = botService;
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken cancellationToken)
    {
        // Store conversation reference for proactive messaging
        var activity = turnContext.Activity;
        _botService.AddOrUpdateConversationReference(activity);

        // Process the message
        var text = turnContext.Activity.Text;
        await turnContext.SendActivityAsync($"You said: {text}", cancellationToken: cancellationToken);
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken cancellationToken)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id != turnContext.Activity.Recipient.Id)
            {
                await turnContext.SendActivityAsync("Welcome to Mystira Bot!", cancellationToken: cancellationToken);
            }
        }
    }
}
```

### 6. Register Services

```csharp
services.AddSingleton<IBotFrameworkHttpAdapter, AdapterWithErrorHandler>();
services.AddSingleton<IBot, TeamsBotHandler>();
services.AddTeamsBot(configuration);
```

## Testing

### Local Development with ngrok

1. Install ngrok: `npm install -g ngrok`
2. Start your app locally: `dotnet run`
3. Expose your local server: `ngrok http 5000`
4. Update Azure Bot messaging endpoint with ngrok URL

### Bot Framework Emulator

1. Download [Bot Framework Emulator](https://github.com/Microsoft/BotFramework-Emulator)
2. Connect to `http://localhost:5000/api/messages/teams`
3. Enter your App ID and Password

## Security Considerations

1. **Validate incoming requests** using Bot Framework authentication
2. **Store credentials securely** in Azure Key Vault
3. **Use HTTPS** for all webhook endpoints
4. **Implement rate limiting** on webhook endpoints

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| 401 Unauthorized | Verify App ID and Password |
| Messages not received | Check messaging endpoint URL |
| Bot not responding | Verify bot is started and connected |
| Timeout errors | Increase DefaultTimeoutSeconds |

### Logging

Enable detailed logging in development:

```json
{
  "Logging": {
    "LogLevel": {
      "Microsoft.Bot": "Debug"
    }
  }
}
```

## References

- [Bot Framework Documentation](https://docs.microsoft.com/en-us/azure/bot-service/)
- [Teams Bot SDK](https://docs.microsoft.com/en-us/microsoftteams/platform/bots/what-are-bots)
- [Azure Bot Service](https://azure.microsoft.com/en-us/services/bot-services/)
