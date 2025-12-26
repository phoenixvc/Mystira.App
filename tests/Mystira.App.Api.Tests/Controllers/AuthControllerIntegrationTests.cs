using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Wolverine;
using Mystira.App.Application.CQRS.Auth.Commands;
using Mystira.App.Application.CQRS.Auth.Responses;
using Mystira.Contracts.App.Requests.Auth;
using Mystira.Contracts.App.Responses.Auth;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace Mystira.App.Api.Tests.Controllers;

public class AuthControllerIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthControllerIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            builder.ConfigureAppConfiguration((context, configBuilder) =>
            {
                configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "JwtSettings:Issuer", "TestIssuer" },
                    { "JwtSettings:Audience", "TestAudience" },
                    { "JwtSettings:SecretKey", "SuperSecretKeyForTestingPurposes123!" },
                    { "InitializeDatabaseOnStartup", "false" }
                });
            });

            builder.ConfigureServices(services =>
            {
                // Mock IMessageBus to isolate the Controller
                var busMock = new Mock<IMessageBus>();
                busMock.Setup(m => m.InvokeAsync<AuthResponse>(
                        It.IsAny<PasswordlessSigninCommand>(),
                        It.IsAny<CancellationToken>(),
                        It.IsAny<TimeSpan?>()))
                    .ReturnsAsync(new AuthResponse(true, "Check your email", "123456"));

                services.AddScoped(_ => busMock.Object);
            });
        });
    }

    [Fact]
    public async Task PasswordlessSignin_ShouldNotThrowInvalidProgramException()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new PasswordlessSigninRequest { Email = "test@example.com" };

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/passwordless/signin", request);

        // Assert
        response.EnsureSuccessStatusCode();
        var content = await response.Content.ReadFromJsonAsync<PasswordlessSigninResponse>();
        content.Should().NotBeNull();
        content!.Success.Should().BeTrue();
    }
}
