using Mystira.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting echo log dictionary data to EchoLog domain object
/// </summary>
public static class EchoLogParser
{
    public static EchoLog Parse(IDictionary<object, object> echoLogDict)
    {
        var echoLog = new EchoLog
        {
            Timestamp = DateTime.UtcNow // Default to current UTC time
        };

        // Parse EchoType (required)
        var echoTypeFound = echoLogDict.TryGetValue("echoType", out var echoTypeObj) ||
                            echoLogDict.TryGetValue("echo_type", out echoTypeObj) ||
                            echoLogDict.TryGetValue("type", out echoTypeObj);

        if (!echoTypeFound || echoTypeObj == null)
        {
            throw new ArgumentException("Required field 'echoType'/'type' is missing or null in echo log data");
        }

        echoLog.EchoType = echoTypeObj.ToString() ?? string.Empty;

        // Parse Description (required)
        var descFound = echoLogDict.TryGetValue("description", out var descObj) ||
                        echoLogDict.TryGetValue("message", out descObj) ||
                        echoLogDict.TryGetValue("text", out descObj);

        if (!descFound || descObj == null)
        {
            throw new ArgumentException("Required field 'description'/'message' is missing or null in echo log data");
        }
        echoLog.Description = descObj.ToString() ?? string.Empty;

        // Parse Strength (with validation, default 0.5)
        var strengthFound = echoLogDict.TryGetValue("strength", out var strengthObj) ||
                            echoLogDict.TryGetValue("power", out strengthObj) ||
                            echoLogDict.TryGetValue("intensity", out strengthObj);

        echoLog.Strength = strengthFound && strengthObj != null &&
                           double.TryParse(strengthObj.ToString(), out double strength)
            ? Math.Clamp(strength, 0.1, 1.0)
            : 0.5;

        // Parse Timestamp if provided (otherwise use default UTC now)
        if ((echoLogDict.TryGetValue("timestamp", out var timestampObj) ||
             echoLogDict.TryGetValue("time", out timestampObj) ||
             echoLogDict.TryGetValue("date", out timestampObj)) &&
            timestampObj != null)
        {
            var timestampStr = timestampObj.ToString();
            if (!string.IsNullOrEmpty(timestampStr) &&
                DateTime.TryParse(timestampStr, out DateTime timestamp))
            {
                echoLog.Timestamp = timestamp;
            }
        }

        return echoLog;
    }
}

