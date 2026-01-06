using Microsoft.JSInterop;
using Moq;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class SettingsServiceTests
{
    private readonly Mock<IJSRuntime> _jsRuntimeMock;
    private readonly SettingsService _sut;

    public SettingsServiceTests()
    {
        _jsRuntimeMock = new Mock<IJSRuntime>();
        _sut = new SettingsService(_jsRuntimeMock.Object);
    }

    [Fact]
    public async Task GetAudioEnabledAsync_ShouldReturnTrueByDefault()
    {
        // Arrange
        _jsRuntimeMock.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.IsAny<object[]>()))
            .ReturnsAsync((string?)null);

        // Act
        var result = await _sut.GetAudioEnabledAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetAudioEnabledAsync_ShouldReturnFalse_WhenStoredAsFalse()
    {
        // Arrange
        _jsRuntimeMock.Setup(x => x.InvokeAsync<string?>("localStorage.getItem", It.Is<object[]>(args => (string)args[0] == "mystira_audio_enabled")))
            .ReturnsAsync("false");

        // Act
        var result = await _sut.GetAudioEnabledAsync();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task SetAudioEnabledAsync_ShouldStoreValue()
    {
        // Act
        await _sut.SetAudioEnabledAsync(false);

        // Assert
        _jsRuntimeMock.Verify(x => x.InvokeAsync<object>(
            "localStorage.setItem",
            It.Is<object[]>(args => (string)args[0] == "mystira_audio_enabled" && (string)args[1] == "false")),
            Times.Once);
    }
}
