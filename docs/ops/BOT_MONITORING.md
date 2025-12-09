# Chat Bot Monitoring Guide

## Overview

This guide covers monitoring and observability for the multi-platform chat bot integration (Discord, Teams, WhatsApp) in Mystira.App.

## Health Checks

### Discord Bot Health Check

The Discord bot includes a built-in health check:

```csharp
services.AddHealthChecks()
    .AddDiscordBotHealthCheck();
```

Endpoint: `/health`

Response:
```json
{
  "status": "Healthy",
  "results": {
    "discord-bot": {
      "status": "Healthy",
      "data": {
        "connected": true,
        "username": "MystiraBot",
        "guilds": 5
      }
    }
  }
}
```

### Custom Health Checks for Teams/WhatsApp

```csharp
public class TeamsBotHealthCheck : IHealthCheck
{
    private readonly IChatBotService _botService;

    public TeamsBotHealthCheck([FromKeyedServices("teams")] IChatBotService botService)
    {
        _botService = botService;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var status = _botService.GetStatus();

        if (!status.IsEnabled)
            return Task.FromResult(HealthCheckResult.Degraded("Teams bot is disabled"));

        if (!status.IsConnected)
            return Task.FromResult(HealthCheckResult.Unhealthy("Teams bot is not connected"));

        return Task.FromResult(HealthCheckResult.Healthy($"Connected with {status.ServerCount} conversations"));
    }
}
```

## Metrics

### Key Metrics to Track

| Metric | Description | Alert Threshold |
|--------|-------------|-----------------|
| `bot.messages.sent` | Messages sent per minute | > 100/min warning |
| `bot.messages.failed` | Failed message attempts | > 5/min critical |
| `bot.latency.ms` | Message send latency | > 5000ms warning |
| `bot.connections` | Active connections | 0 = critical |
| `bot.rate_limits` | Rate limit hits | > 10/min warning |

### Application Insights Integration

```csharp
public class TelemetryBotService : IChatBotService
{
    private readonly IChatBotService _inner;
    private readonly TelemetryClient _telemetry;

    public async Task SendMessageAsync(ulong channelId, string message, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _inner.SendMessageAsync(channelId, message, cancellationToken);

            _telemetry.TrackMetric("bot.messages.sent", 1);
            _telemetry.TrackMetric("bot.latency.ms", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _telemetry.TrackMetric("bot.messages.failed", 1);
            _telemetry.TrackException(ex);
            throw;
        }
    }
}
```

### Prometheus Metrics

```csharp
public static class BotMetrics
{
    public static readonly Counter MessagesSent = Metrics.CreateCounter(
        "bot_messages_sent_total",
        "Total messages sent",
        new CounterConfiguration { LabelNames = new[] { "platform" } });

    public static readonly Counter MessagesFailed = Metrics.CreateCounter(
        "bot_messages_failed_total",
        "Total failed message attempts",
        new CounterConfiguration { LabelNames = new[] { "platform", "error_type" } });

    public static readonly Histogram MessageLatency = Metrics.CreateHistogram(
        "bot_message_latency_seconds",
        "Message send latency",
        new HistogramConfiguration { LabelNames = new[] { "platform" } });
}
```

## Logging

### Structured Logging Configuration

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Mystira.App.Infrastructure.Discord": "Debug",
        "Mystira.App.Infrastructure.Teams": "Debug",
        "Mystira.App.Infrastructure.WhatsApp": "Debug"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "ApplicationInsights",
        "Args": {
          "connectionString": "<your-connection-string>"
        }
      }
    ]
  }
}
```

### Key Log Events

| Event | Level | Description |
|-------|-------|-------------|
| `Sent message to channel {ChannelId}` | Debug | Successful message send |
| `Rate limited while sending message` | Warning | Hit rate limit |
| `Failed to send message` | Error | Message send failure |
| `Bot disconnected` | Warning | Connection lost |
| `Bot reconnected` | Information | Connection restored |

## Alerting

### Azure Monitor Alerts

```json
{
  "alertRule": {
    "name": "Bot Disconnection Alert",
    "description": "Alert when any chat bot becomes disconnected",
    "condition": {
      "query": "customMetrics | where name == 'bot.connections' | where value == 0",
      "timeAggregation": "Count",
      "operator": "GreaterThan",
      "threshold": 0
    },
    "actions": [
      {
        "actionGroupId": "/subscriptions/.../actionGroups/ops-team"
      }
    ]
  }
}
```

### PagerDuty Integration

```csharp
public class BotAlertService
{
    private readonly HttpClient _httpClient;

    public async Task SendCriticalAlert(string message)
    {
        var payload = new
        {
            routing_key = "<pagerduty-routing-key>",
            event_action = "trigger",
            payload = new
            {
                summary = message,
                source = "mystira-bot",
                severity = "critical"
            }
        };

        await _httpClient.PostAsJsonAsync(
            "https://events.pagerduty.com/v2/enqueue",
            payload);
    }
}
```

## Dashboard Examples

### Grafana Dashboard Panels

1. **Bot Status Overview**
   - Connection status per platform
   - Uptime percentage
   - Current server/conversation count

2. **Message Volume**
   - Messages sent/received per hour
   - By platform breakdown
   - Success/failure ratio

3. **Performance**
   - P50/P95/P99 latency
   - Rate limit frequency
   - Error rate trends

### Sample Grafana Query

```promql
# Message success rate by platform
sum(rate(bot_messages_sent_total[5m])) by (platform)
/
(sum(rate(bot_messages_sent_total[5m])) by (platform) + sum(rate(bot_messages_failed_total[5m])) by (platform))
* 100
```

## Runbook: Common Issues

### Bot Not Responding

1. Check health endpoint: `curl https://your-app/health`
2. Verify bot credentials in Key Vault
3. Check Discord/Teams/WhatsApp service status
4. Review recent error logs
5. Restart bot service if needed

### High Latency

1. Check message queue depth
2. Review rate limit metrics
3. Check external API latency
4. Consider scaling out

### Memory Issues

1. Monitor event handler count (possible leak)
2. Check conversation cache size
3. Review for unbounded collections
4. Force garbage collection if needed

## References

- [Application Insights](https://docs.microsoft.com/en-us/azure/azure-monitor/app/app-insights-overview)
- [Prometheus .NET](https://github.com/prometheus-net/prometheus-net)
- [Grafana Dashboards](https://grafana.com/docs/grafana/latest/dashboards/)
