using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

[JsonConverter(typeof(StringEnumJsonConverter<FantasyTheme>))]
public class FantasyTheme : StringEnum<FantasyTheme>
{
    public FantasyTheme(string value) : base(value)
    {
    }
}
