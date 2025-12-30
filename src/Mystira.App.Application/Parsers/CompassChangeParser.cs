using Mystira.Domain.Models;

namespace Mystira.App.Application.Parsers;

/// <summary>
/// Parser for converting compass change dictionary data to CompassChange domain object
/// </summary>
public static class CompassChangeParser
{
    public static CompassChange Parse(IDictionary<object, object> compassChangeDict)
    {
        var compassChange = new CompassChange();

        // Parse Axis (required)
        var axisFound = compassChangeDict.TryGetValue("axis", out var axisObj) ||
                        compassChangeDict.TryGetValue("compass_axis", out axisObj) ||
                        compassChangeDict.TryGetValue("value", out axisObj);

        if (!axisFound || axisObj == null)
        {
            throw new ArgumentException("Required field 'axis' is missing or null in compass change data");
        }
        compassChange.Axis = axisObj.ToString() ?? string.Empty;

        // Parse Delta (required) with validation
        var deltaFound = compassChangeDict.TryGetValue("delta", out var deltaObj) ||
                         compassChangeDict.TryGetValue("change", out deltaObj) ||
                         compassChangeDict.TryGetValue("impact", out deltaObj) ||
                         compassChangeDict.TryGetValue("value", out deltaObj);

        if (!deltaFound || deltaObj == null || !double.TryParse(deltaObj.ToString(), out double delta))
        {
            throw new ArgumentException("Required field 'delta'/'change'/'impact' is invalid or null in compass change data");
        }
        // Validate delta is between -1.0 and 1.0
        compassChange.Delta = Math.Clamp(delta, -1.0, 1.0);

        if (compassChangeDict.TryGetValue("developmental_link", out var devLinkObj) ||
            compassChangeDict.TryGetValue("developmentalLink", out devLinkObj))
        {
            compassChange.DevelopmentalLink = devLinkObj?.ToString();
        }

        return compassChange;
    }
}

