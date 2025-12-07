using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Services;

public class ApiConnectionHelperTests
{
    [Fact]
    public void IsConnectionRefused_WithFailedToFetchMessage_ReturnsTrue()
    {
        // Arrange
        var ex = new HttpRequestException("TypeError: Failed to fetch");

        // Act
        var result = ApiConnectionHelper.IsConnectionRefused(ex);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConnectionRefused_WithConnectionRefusedMessage_ReturnsTrue()
    {
        // Arrange
        var ex = new HttpRequestException("Connection refused");

        // Act
        var result = ApiConnectionHelper.IsConnectionRefused(ex);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConnectionRefused_WithNetworkErrorMessage_ReturnsTrue()
    {
        // Arrange
        var ex = new HttpRequestException("Network error occurred");

        // Act
        var result = ApiConnectionHelper.IsConnectionRefused(ex);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConnectionRefused_WithInnerFailedToFetch_ReturnsTrue()
    {
        // Arrange
        var innerEx = new Exception("Failed to fetch");
        var ex = new HttpRequestException("Some error", innerEx);

        // Act
        var result = ApiConnectionHelper.IsConnectionRefused(ex);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsConnectionRefused_WithOtherError_ReturnsFalse()
    {
        // Arrange
        var ex = new HttpRequestException("404 Not Found");

        // Act
        var result = ApiConnectionHelper.IsConnectionRefused(ex);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetConnectionErrorMessage_InDevelopmentWithLocalhost_ReturnsHelpfulMessage()
    {
        // Arrange
        var apiBaseUrl = "http://localhost:5260/";
        var isDevelopment = true;

        // Act
        var message = ApiConnectionHelper.GetConnectionErrorMessage(apiBaseUrl, isDevelopment);

        // Assert
        Assert.Contains("localhost", message);
        Assert.Contains("dotnet run", message);
        Assert.Contains("API is running", message);
    }

    [Fact]
    public void GetConnectionErrorMessage_InDevelopmentWithoutLocalhost_ReturnsGenericMessage()
    {
        // Arrange
        var apiBaseUrl = "https://api.example.com/";
        var isDevelopment = true;

        // Act
        var message = ApiConnectionHelper.GetConnectionErrorMessage(apiBaseUrl, isDevelopment);

        // Assert
        Assert.Contains("check your internet connection", message);
        Assert.DoesNotContain("dotnet run", message);
    }

    [Fact]
    public void GetConnectionErrorMessage_InProduction_ReturnsGenericMessage()
    {
        // Arrange
        var apiBaseUrl = "http://localhost:5260/";
        var isDevelopment = false;

        // Act
        var message = ApiConnectionHelper.GetConnectionErrorMessage(apiBaseUrl, isDevelopment);

        // Assert
        Assert.Contains("check your internet connection", message);
        Assert.DoesNotContain("dotnet run", message);
    }

    [Fact]
    public void GetConnectionLogMessage_InDevelopmentWithLocalhost_ReturnsDetailedMessage()
    {
        // Arrange
        var apiBaseUrl = "http://localhost:5260/";
        var isDevelopment = true;

        // Act
        var message = ApiConnectionHelper.GetConnectionLogMessage(apiBaseUrl, isDevelopment);

        // Assert
        Assert.Contains("localhost", message);
        Assert.Contains("may not be running", message);
        Assert.Contains("dotnet run", message);
    }

    [Fact]
    public void GetConnectionLogMessage_InProduction_ReturnsGenericMessage()
    {
        // Arrange
        var apiBaseUrl = "https://api.example.com/";
        var isDevelopment = false;

        // Act
        var message = ApiConnectionHelper.GetConnectionLogMessage(apiBaseUrl, isDevelopment);

        // Assert
        Assert.Contains("Network error", message);
        Assert.DoesNotContain("dotnet run", message);
    }
}
