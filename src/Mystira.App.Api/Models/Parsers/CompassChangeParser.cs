using Mystira.App.Domain.Models;

namespace Mystira.App.Api.Models.Parsers;

/// <summary>
/// Parser for converting compass change dictionary data to CompassChange domain object
/// </summary>
public static class CompassChangeParser
{
    public static CompassChange Parse(IDictionary<object, object> compassChangeDict)
    {
        var compassChange = new CompassChange();

        // Parse Axis (required)
        if (compassChangeDict.TryGetValue("axis", out var axisObj) ||
            compassChangeDict.TryGetValue("compass_axis", out axisObj) ||
            compassChangeDict.TryGetValue("value", out axisObj))
        {
            if (axisObj != null)
            {
                compassChange.Axis = axisObj.ToString() ?? string.Empty;
            }
            else
            {
                throw new ArgumentException("Required field 'axis' is missing or null in compass change data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'axis' is missing in compass change data");
        }

        // Parse Delta (required) with validation
        if (compassChangeDict.TryGetValue("delta", out var deltaObj) ||
            compassChangeDict.TryGetValue("change", out deltaObj) ||
            compassChangeDict.TryGetValue("impact", out deltaObj) ||
            compassChangeDict.TryGetValue("value", out deltaObj))
        {
            if (deltaObj != null &&
                double.TryParse(deltaObj.ToString(), out double delta))
            {
                // Validate delta is between -1.0 and 1.0
                compassChange.Delta = Math.Clamp(delta, -1.0, 1.0);
            }
            else
            {
                throw new ArgumentException("Required field 'delta'/'change'/'impact' is invalid or null in compass change data");
            }
        }
        else
        {
            throw new ArgumentException("Required field 'delta'/'change'/'impact' is missing in compass change data");
        }

        if (compassChangeDict.TryGetValue("developmental_link", out var devLinkObj) ||
            compassChangeDict.TryGetValue("developmentalLink", out devLinkObj))
        {
            compassChange.DevelopmentalLink = devLinkObj?.ToString();
        }

        return compassChange;
    }
}

