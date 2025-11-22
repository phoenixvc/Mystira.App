using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models.Parsers;

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
        if (echoLogDict.TryGetValue("echoType", out var echoTypeObj) ||
            echoLogDict.TryGetValue("echo_type", out echoTypeObj) ||
            echoLogDict.TryGetValue("type", out echoTypeObj))
        {
            if (echoTypeObj != null)
            {
                var parsed = EchoType.Parse(echoTypeObj.ToString());
                if (parsed == null)
                {
                    throw new ArgumentException($"Invalid EchoType: {echoTypeObj}");
                }

                echoLog.EchoType = parsed;
            }
            else
            {
                throw new ArgumentException("Required field 'echoType'/'type' is missing or null in echo log data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'echoType'/'type' is missing in echo log data");
        }

        // Parse Description (required)
        if (echoLogDict.TryGetValue("description", out var descObj) ||
            echoLogDict.TryGetValue("message", out descObj) ||
            echoLogDict.TryGetValue("text", out descObj))
        {
            if (descObj != null)
            {
                echoLog.Description = descObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'description'/'message' is missing or null in echo log data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'description'/'message' is missing in echo log data");
        }

        // Parse Strength (with validation)
        if (echoLogDict.TryGetValue("strength", out var strengthObj) ||
            echoLogDict.TryGetValue("power", out strengthObj) ||
            echoLogDict.TryGetValue("intensity", out strengthObj))
        {
            if (strengthObj != null &&
                double.TryParse(strengthObj.ToString(), out double strength))
            {
                // Validate strength is between 0.1 and 1.0
                echoLog.Strength = Math.Clamp(strength, 0.1, 1.0);
            }
            else
            {
                // Default to mid-range if not specified or invalid
                echoLog.Strength = 0.5;
            }
        }
        else
        {
            // Default to mid-range if not specified
            echoLog.Strength = 0.5;
        }

        // Parse Timestamp if provided (otherwise use default UTC now)
        if (echoLogDict.TryGetValue("timestamp", out var timestampObj) ||
            echoLogDict.TryGetValue("time", out timestampObj) ||
            echoLogDict.TryGetValue("date", out timestampObj))
        {
            if (timestampObj != null)
            {
                var timestampStr = timestampObj.ToString();
                if (!string.IsNullOrEmpty(timestampStr) &&
                    DateTime.TryParse(timestampStr, out DateTime timestamp))
                {
                    echoLog.Timestamp = timestamp;
                }
            }
        }

        return echoLog;
    }
}

