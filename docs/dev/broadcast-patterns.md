# Broadcast and First-Responder Patterns

## Overview

The chat bot services support broadcasting messages to multiple channels and awaiting responses. This is useful for:

- **Support escalation**: Notify multiple support agents, first to respond handles the ticket
- **Load balancing**: Distribute work to whoever is available first
- **Failover**: Try multiple channels, use whichever responds
- **Announcements**: Send to multiple channels with response tracking

## Available Methods

### SendAndAwaitFirstResponseAsync

Broadcasts a text message and returns when the first response is received.

```csharp
var result = await chatBot.SendAndAwaitFirstResponseAsync(
    channelIds: new[] { 123UL, 456UL, 789UL },
    message: "New support ticket: User needs help with login",
    timeout: TimeSpan.FromMinutes(5),
    cancellationToken: ct);

if (!result.TimedOut)
{
    Console.WriteLine($"Handled by {result.ResponderName} in {result.ResponseTime}");
}
```

### SendEmbedAndAwaitFirstResponseAsync

Same as above but with rich embed content.

```csharp
var embed = new EmbedData
{
    Title = "Support Ticket #1234",
    Description = "User cannot log in",
    ColorRed = 255, ColorGreen = 165, ColorBlue = 0,
    Fields = new List<EmbedFieldData>
    {
        new("Priority", "High", true),
        new("Category", "Authentication", true)
    }
};

var result = await chatBot.SendEmbedAndAwaitFirstResponseAsync(
    channelIds: supportChannels,
    embed: embed,
    timeout: TimeSpan.FromMinutes(5));
```

### BroadcastWithResponseHandlerAsync

For more control, use a callback handler:

```csharp
var responses = new List<ResponseEvent>();

await chatBot.BroadcastWithResponseHandlerAsync(
    channelIds: supportChannels,
    message: "Who can help with this issue?",
    onResponse: async response =>
    {
        responses.Add(response);

        // Return true to stop listening after first "I'll take it"
        if (response.Content.Contains("I'll take it", StringComparison.OrdinalIgnoreCase))
        {
            await NotifyAssignment(response.ResponderId);
            return true; // Stop listening
        }

        return false; // Keep listening
    },
    timeout: TimeSpan.FromMinutes(10));
```

## Response Data

### FirstResponderResult

```csharp
public class FirstResponderResult
{
    public bool TimedOut { get; set; }           // True if no response received
    public ulong RespondingChannelId { get; set; }
    public string? RespondingChannelName { get; set; }
    public ulong ResponseMessageId { get; set; }
    public string? ResponseContent { get; set; }
    public ulong ResponderId { get; set; }       // User ID of responder
    public string? ResponderName { get; set; }   // Username of responder
    public TimeSpan ResponseTime { get; set; }   // Time to first response
    public IReadOnlyList<SentMessage> SentMessages { get; set; } // Messages sent
}
```

### ResponseEvent (for handler callback)

```csharp
public class ResponseEvent
{
    public ulong ChannelId { get; set; }
    public string? ChannelName { get; set; }
    public ulong MessageId { get; set; }
    public string? Content { get; set; }
    public ulong ResponderId { get; set; }
    public string? ResponderName { get; set; }
    public TimeSpan ElapsedTime { get; set; }
    public ulong? ReplyToMessageId { get; set; }
    public bool IsDirectReply { get; set; }      // True if reply to broadcast
}
```

## Platform Support

| Feature | Discord | Teams | WhatsApp |
|---------|---------|-------|----------|
| Real-time response | Yes | No | No |
| First responder | Yes | Messages only | Messages only |
| Response handler | Yes | No | No |
| Direct reply detection | Yes | No | No |

### Discord

Full support with WebSocket-based real-time message monitoring:

```csharp
// Discord can detect responses in real-time
_client.MessageReceived += OnMessageReceived;
```

### Teams / WhatsApp

Limited support - messages are sent but response monitoring requires webhook-based integration:

```csharp
// Messages sent successfully, but response tracking not available
_logger.LogWarning("Platform does not support real-time response monitoring");
return new FirstResponderResult { TimedOut = true, SentMessages = sentMessages };
```

## Usage Patterns

### Support Escalation

```csharp
public async Task EscalateTicketAsync(Ticket ticket)
{
    var supportChannels = await GetSupportChannelIds();

    var embed = new EmbedData
    {
        Title = $"Escalated Ticket #{ticket.Id}",
        Description = ticket.Description,
        ColorRed = 255, ColorGreen = 0, ColorBlue = 0,
        Footer = "Reply to claim this ticket"
    };

    var result = await _chatBot.SendEmbedAndAwaitFirstResponseAsync(
        supportChannels,
        embed,
        TimeSpan.FromMinutes(15));

    if (!result.TimedOut)
    {
        await AssignTicket(ticket.Id, result.ResponderId);
        await NotifyAssignment(result.RespondingChannelId, ticket, result.ResponderName);
    }
    else
    {
        await EscalateToManager(ticket);
    }
}
```

### Load Balancing

```csharp
public async Task<ulong?> FindAvailableWorkerAsync(string task)
{
    var workerChannels = await GetWorkerDMChannels();

    var result = await _chatBot.SendAndAwaitFirstResponseAsync(
        workerChannels,
        $"New task available: {task}\nReply 'accept' to take it.",
        TimeSpan.FromMinutes(2));

    if (!result.TimedOut && result.ResponseContent?.Contains("accept") == true)
    {
        return result.ResponderId;
    }

    return null;
}
```

### Collecting Multiple Responses

```csharp
public async Task<List<string>> CollectFeedbackAsync(string question)
{
    var responses = new List<string>();
    var respondedUsers = new HashSet<ulong>();

    await _chatBot.BroadcastWithResponseHandlerAsync(
        teamChannels,
        question,
        async response =>
        {
            // Deduplicate by user
            if (respondedUsers.Add(response.ResponderId))
            {
                responses.Add(response.Content ?? "");
            }

            // Keep collecting until timeout
            return false;
        },
        TimeSpan.FromHours(1));

    return responses;
}
```

## Best Practices

1. **Set reasonable timeouts** - Don't wait forever; have fallback behavior
2. **Handle TimedOut gracefully** - Always check `result.TimedOut`
3. **Use embeds for important broadcasts** - Better visibility
4. **Log response times** - Track team response performance
5. **Deduplicate responses** - Same user may respond multiple times
6. **Consider platform limitations** - Teams/WhatsApp don't support real-time

## Error Handling

```csharp
try
{
    var result = await _chatBot.SendAndAwaitFirstResponseAsync(...);

    if (result.TimedOut)
    {
        // No response within timeout
        await HandleNoResponse();
    }
    else
    {
        // Process response
        await ProcessResponse(result);
    }
}
catch (InvalidOperationException ex) when (ex.Message.Contains("not connected"))
{
    // Bot disconnected
    _logger.LogError(ex, "Bot disconnected during broadcast");
    await ReconnectAndRetry();
}
catch (OperationCanceledException)
{
    // Cancelled by token
    _logger.LogInformation("Broadcast cancelled");
}
```

## See Also

- [THREAD_SAFETY.md](./THREAD_SAFETY.md) - Thread safety in handlers
- [MULTI_PLATFORM_CHAT_BOT_SETUP.md](../setup/MULTI_PLATFORM_CHAT_BOT_SETUP.md) - Service configuration
- [ADDING_NEW_CHAT_PLATFORM.md](./ADDING_NEW_CHAT_PLATFORM.md) - Implementing for new platforms
