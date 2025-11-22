using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

[JsonConverter(typeof(StringEnumJsonConverter<CoreAxis>))]
public class CoreAxis : StringEnum<CoreAxis>
{
    public CoreAxis(string value) : base(value)
    {
    }
}
