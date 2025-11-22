using System.Text.Json.Serialization;

namespace Mystira.App.Domain.Models;

[JsonConverter(typeof(StringEnumJsonConverter<EchoType>))]
public class EchoType : StringEnum<EchoType>
{
    public EchoType(string value) : base(value)
    {
    }
}
