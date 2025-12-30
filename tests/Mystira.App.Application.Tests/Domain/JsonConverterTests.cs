using System.Text.Json;
using FluentAssertions;
using Mystira.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.Domain;

public class JsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    #region CoreAxisJsonConverter Tests

    [Fact]
    public void CoreAxisJsonConverter_Serialize_WritesStringValue()
    {
        // Arrange
        var coreAxis = new CoreAxis("courage");

        // Act
        var json = JsonSerializer.Serialize(coreAxis, _options);

        // Assert
        json.Should().Be("\"courage\"");
    }

    [Fact]
    public void CoreAxisJsonConverter_Deserialize_ReadsStringValue()
    {
        // Arrange
        var json = "\"wisdom\"";

        // Act
        var coreAxis = JsonSerializer.Deserialize<CoreAxis>(json, _options);

        // Assert
        coreAxis.Should().NotBeNull();
        coreAxis!.Value.Should().Be("wisdom");
    }

    [Fact]
    public void CoreAxisJsonConverter_Deserialize_NullValue_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var coreAxis = JsonSerializer.Deserialize<CoreAxis>(json, _options);

        // Assert
        coreAxis.Should().BeNull();
    }

    [Fact]
    public void CoreAxisJsonConverter_Serialize_NullValue_WritesNull()
    {
        // Arrange
        CoreAxis? coreAxis = null;

        // Act
        var json = JsonSerializer.Serialize(coreAxis, _options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void CoreAxisJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var original = new CoreAxis("empathy");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<CoreAxis>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(original.Value);
    }

    [Fact]
    public void CoreAxisJsonConverter_InObject_SerializesCorrectly()
    {
        // Arrange
        var testObject = new TestCoreAxisContainer { Axis = new CoreAxis("kindness") };

        // Act
        var json = JsonSerializer.Serialize(testObject, _options);
        var deserialized = JsonSerializer.Deserialize<TestCoreAxisContainer>(json, _options);

        // Assert
        json.Should().Contain("\"axis\":\"kindness\"");
        deserialized.Should().NotBeNull();
        deserialized!.Axis.Should().NotBeNull();
        deserialized.Axis!.Value.Should().Be("kindness");
    }

    private class TestCoreAxisContainer
    {
        public CoreAxis? Axis { get; set; }
    }

    #endregion

    #region EchoTypeJsonConverter Tests

    [Fact]
    public void EchoTypeJsonConverter_Serialize_WritesStringValue()
    {
        // Arrange
        var echoType = new EchoType("discovery");

        // Act
        var json = JsonSerializer.Serialize(echoType, _options);

        // Assert
        json.Should().Be("\"discovery\"");
    }

    [Fact]
    public void EchoTypeJsonConverter_Deserialize_ReadsStringValue()
    {
        // Arrange
        var json = "\"challenge\"";

        // Act
        var echoType = JsonSerializer.Deserialize<EchoType>(json, _options);

        // Assert
        echoType.Should().NotBeNull();
        echoType!.Value.Should().Be("challenge");
    }

    [Fact]
    public void EchoTypeJsonConverter_Deserialize_NullValue_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var echoType = JsonSerializer.Deserialize<EchoType>(json, _options);

        // Assert
        echoType.Should().BeNull();
    }

    [Fact]
    public void EchoTypeJsonConverter_Serialize_NullValue_WritesNull()
    {
        // Arrange
        EchoType? echoType = null;

        // Act
        var json = JsonSerializer.Serialize(echoType, _options);

        // Assert
        json.Should().Be("null");
    }

    [Fact]
    public void EchoTypeJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var original = new EchoType("transformation");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<EchoType>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(original.Value);
    }

    #endregion

    #region ArchetypeJsonConverter Tests

    [Fact]
    public void ArchetypeJsonConverter_Serialize_WritesStringValue()
    {
        // Arrange
        var archetype = new Archetype("hero");

        // Act
        var json = JsonSerializer.Serialize(archetype, _options);

        // Assert
        json.Should().Be("\"hero\"");
    }

    [Fact]
    public void ArchetypeJsonConverter_Deserialize_ReadsStringValue()
    {
        // Arrange
        var json = "\"mentor\"";

        // Act
        var archetype = JsonSerializer.Deserialize<Archetype>(json, _options);

        // Assert
        archetype.Should().NotBeNull();
        archetype!.Value.Should().Be("mentor");
    }

    [Fact]
    public void ArchetypeJsonConverter_Deserialize_NullValue_ReturnsNull()
    {
        // Arrange
        var json = "null";

        // Act
        var archetype = JsonSerializer.Deserialize<Archetype>(json, _options);

        // Assert
        archetype.Should().BeNull();
    }

    [Fact]
    public void ArchetypeJsonConverter_RoundTrip_PreservesValue()
    {
        // Arrange
        var original = new Archetype("trickster");

        // Act
        var json = JsonSerializer.Serialize(original, _options);
        var deserialized = JsonSerializer.Deserialize<Archetype>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Value.Should().Be(original.Value);
    }

    #endregion

    #region Complex Object Serialization Tests

    [Fact]
    public void Scenario_WithCompassChange_SerializesAndDeserializesCorrectly()
    {
        // Arrange
        var scenario = new Scenario
        {
            Id = "test-scenario",
            Title = "Test",
            Scenes = new List<Scene>
            {
                new Scene
                {
                    Id = "scene-1",
                    Title = "Test Scene",
                    Branches = new List<Branch>
                    {
                        new Branch
                        {
                            Choice = "Be brave",
                            NextSceneId = "scene-2",
                            CompassChange = new CompassChange
                            {
                                Axis = "courage",
                                Delta = 1.0
                            }
                        }
                    }
                }
            }
        };

        // Act
        var json = JsonSerializer.Serialize(scenario, _options);
        var deserialized = JsonSerializer.Deserialize<Scenario>(json, _options);

        // Assert
        deserialized.Should().NotBeNull();
        deserialized!.Scenes.Should().HaveCount(1);
        deserialized.Scenes![0].Branches.Should().HaveCount(1);
        deserialized.Scenes[0].Branches![0].Choice.Should().Be("Be brave");
        deserialized.Scenes[0].Branches![0].CompassChange.Should().NotBeNull();
        deserialized.Scenes[0].Branches![0].CompassChange!.Axis.Should().Be("courage");
    }

    [Fact]
    public void ListOfCoreAxis_SerializesAsArrayOfStrings()
    {
        // Arrange
        var axes = new List<CoreAxis>
        {
            new CoreAxis("courage"),
            new CoreAxis("wisdom"),
            new CoreAxis("empathy")
        };

        // Act
        var json = JsonSerializer.Serialize(axes, _options);
        var deserialized = JsonSerializer.Deserialize<List<CoreAxis>>(json, _options);

        // Assert
        json.Should().Be("[\"courage\",\"wisdom\",\"empathy\"]");
        deserialized.Should().HaveCount(3);
        deserialized![0].Value.Should().Be("courage");
        deserialized[1].Value.Should().Be("wisdom");
        deserialized[2].Value.Should().Be("empathy");
    }

    #endregion
}
