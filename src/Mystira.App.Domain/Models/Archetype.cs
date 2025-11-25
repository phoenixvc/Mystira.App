using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

[JsonConverter(typeof(StringEnumJsonConverter<Archetype>))]
public class Archetype : StringEnum<Archetype>
{
    public Archetype(string value) : base(value)
    {
    }
}
