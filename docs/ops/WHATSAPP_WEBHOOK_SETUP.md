# WhatsApp Webhook Setup Guide (Azure Communication Services)

## Overview

This guide covers setting up webhooks for the WhatsApp integration using Azure Communication Services in Mystira.App.

## Prerequisites

- Azure subscription
- Azure Communication Services resource
- WhatsApp Business Account (through Meta Business Suite)
- Azure Communication Services WhatsApp channel configured

## Configuration Steps

### 1. Create Azure Communication Services Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Create a new **Communication Services** resource
3. Note down the **Connection String** from Keys section

### 2. Set Up WhatsApp Channel

1. In your Communication Services resource, go to **Channels** > **WhatsApp**
2. Connect your WhatsApp Business Account
3. Complete the Meta verification process
4. Note down the **Channel Registration ID**

### 3. Update appsettings.json

```json
{
  "WhatsApp": {
    "Enabled": true,
    "ConnectionString": "<your-acs-connection-string>",
    "ChannelRegistrationId": "<your-channel-registration-id>",
    "PhoneNumberId": "<your-phone-number-id>",
    "MaxRetryAttempts": 3,
    "DefaultTimeoutSeconds": 30,
    "WebhookUrl": "https://your-domain.com/api/webhooks/whatsapp",
    "WebhookVerifyToken": "<your-verify-token>"
  }
}
```

### 4. Create Webhook Controller

```csharp
[ApiController]
[Route("api/webhooks")]
public class WhatsAppWebhookController : ControllerBase
{
    private readonly WhatsAppBotService _botService;
    private readonly ILogger<WhatsAppWebhookController> _logger;

    public WhatsAppWebhookController(
        WhatsAppBotService botService,
        ILogger<WhatsAppWebhookController> logger)
    {
        _botService = botService;
        _logger = logger;
    }

    // Webhook verification (GET)
    [HttpGet("whatsapp")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")] string mode,
        [FromQuery(Name = "hub.verify_token")] string verifyToken,
        [FromQuery(Name = "hub.challenge")] string challenge)
    {
        if (mode == "subscribe" && verifyToken == _options.WebhookVerifyToken)
        {
            return Ok(challenge);
        }
        return Unauthorized();
    }

    // Incoming messages (POST)
    [HttpPost("whatsapp")]
    public async Task<IActionResult> ReceiveMessage([FromBody] JsonElement payload)
    {
        try
        {
            // Parse the incoming message
            var entry = payload.GetProperty("entry")[0];
            var changes = entry.GetProperty("changes")[0];
            var value = changes.GetProperty("value");

            if (value.TryGetProperty("messages", out var messages))
            {
                foreach (var message in messages.EnumerateArray())
                {
                    var from = message.GetProperty("from").GetString();
                    var text = message.GetProperty("text").GetProperty("body").GetString();

                    // Register conversation for proactive messaging
                    _botService.RegisterConversation(from!);

                    _logger.LogInformation("Received WhatsApp message from {From}: {Text}", from, text);

                    // Process the message...
                }
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing WhatsApp webhook");
            return StatusCode(500);
        }
    }
}
```

### 5. Configure Event Grid (Alternative)

Azure Communication Services can also send events via Event Grid:

1. Create an Event Grid subscription in your ACS resource
2. Select **Microsoft.Communication.SMSReceived** or relevant events
3. Point to your webhook endpoint

```csharp
[HttpPost("whatsapp/eventgrid")]
public async Task<IActionResult> HandleEventGrid([FromBody] EventGridEvent[] events)
{
    foreach (var eventGridEvent in events)
    {
        if (eventGridEvent.EventType == "Microsoft.Communication.SMSReceived")
        {
            var data = eventGridEvent.Data.ToObjectFromJson<SmsReceivedEventData>();
            // Process SMS...
        }
    }
    return Ok();
}
```

## 24-Hour Window Considerations

WhatsApp has a 24-hour messaging window policy:

1. **User-initiated conversations**: You can send any message within 24 hours of the last user message
2. **Business-initiated conversations**: Outside the 24-hour window, you must use approved template messages

### Using Template Messages

```csharp
await _botService.SendTemplateMessageAsync(
    phoneNumber: "+1234567890",
    templateName: "order_confirmation",
    templateLanguage: "en",
    parameters: new[] { "Order #12345", "Dec 15, 2024" }
);
```

## Security Considerations

1. **Validate webhook signatures** from Meta
2. **Store credentials securely** in Azure Key Vault
3. **Use HTTPS** for all webhook endpoints
4. **Implement rate limiting** to prevent abuse
5. **Validate phone number formats** (E.164)

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Webhook verification fails | Check verify token matches |
| Messages not delivered | Verify 24-hour window or use templates |
| 401/403 errors | Check connection string and channel ID |
| Rate limiting | Reduce message frequency, add delays |

### Logging

Enable detailed logging:

```json
{
  "Logging": {
    "LogLevel": {
      "Mystira.App.Infrastructure.WhatsApp": "Debug",
      "Azure.Communication": "Debug"
    }
  }
}
```

## Message Templates

Templates must be pre-approved by Meta. Create them in:
1. Meta Business Suite > WhatsApp Manager > Message Templates
2. Or via the WhatsApp Business API

Example template:
```
Name: order_status
Language: en
Body: Your order {{1}} has been {{2}}. Track at: {{3}}
```

## References

- [Azure Communication Services WhatsApp](https://docs.microsoft.com/en-us/azure/communication-services/concepts/whatsapp)
- [WhatsApp Business API](https://developers.facebook.com/docs/whatsapp/business-api/)
- [Meta Webhooks](https://developers.facebook.com/docs/whatsapp/webhooks/)
