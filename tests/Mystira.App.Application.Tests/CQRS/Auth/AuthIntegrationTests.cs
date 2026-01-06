using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Mystira.App.Application.CQRS.Auth.Commands;
using Mystira.App.Application.Ports.Auth;
using Mystira.App.Application.Ports.Data;
using Mystira.App.Domain.Models;
using Xunit;

namespace Mystira.App.Application.Tests.CQRS.Auth;

public class AuthIntegrationTests : CqrsIntegrationTestBase
{
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly Mock<IPendingSignupRepository> _pendingSignupRepositoryMock = new();

    public AuthIntegrationTests()
    {
        // Re-configure ServiceProvider to include mocks for external ports not in base class
        var services = new ServiceCollection();

        // Use the same setup as base but with additional mocks
        // (Note: CqrsIntegrationTestBase doesn't expose its ServiceCollection, so we replicate enough)

        // This is a bit of a hack to get a working MediatR with the CachingBehavior
        // while mocking the dependencies of RequestPasswordlessSigninCommandHandler
    }

    [Fact]
    public async Task RequestPasswordlessSignin_ShouldNotThrowBadImageFormatException()
    {
        // We use the ServiceProvider from the base class but we need to ensure the handler can be resolved.
        // The base class registers all handlers from the Application assembly.
        // RequestPasswordlessSigninCommandHandler depends on:
        // IAccountRepository (registered in base)
        // IPendingSignupRepository (NOT registered in base)
        // IUnitOfWork (registered in base)
        // IEmailService (NOT registered in base)
        // ILogger (registered in base)

        var services = new ServiceCollection();
        // Add minimal services to reproduce
        services.AddMemoryCache();
        services.AddSingleton<Mystira.App.Application.Services.IQueryCacheInvalidationService, Mystira.App.Application.Services.QueryCacheInvalidationService>();

        // Add Mocks
        var accountRepoMock = new Mock<IAccountRepository>();
        var pendingSignupRepoMock = new Mock<IPendingSignupRepository>();
        var unitOfWorkMock = new Mock<IUnitOfWork>();
        var emailServiceMock = new Mock<IEmailService>();

        services.AddSingleton(accountRepoMock.Object);
        services.AddSingleton(pendingSignupRepoMock.Object);
        services.AddSingleton(unitOfWorkMock.Object);
        services.AddSingleton(emailServiceMock.Object);
        services.AddLogging();

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(RequestPasswordlessSigninCommand).Assembly);
            cfg.AddOpenBehavior(typeof(Mystira.App.Application.Behaviors.QueryCachingBehavior<,>));
        });

        var sp = services.BuildServiceProvider();
        var mediator = sp.GetRequiredService<IMediator>();

        var command = new RequestPasswordlessSigninCommand("test@example.com");

        // Act & Assert
        // If the issue exists, this should throw BadImageFormatException during handler resolution/invocation
        Func<Task> act = async () => await mediator.Send(command);

        await act.Should().NotThrowAsync<System.BadImageFormatException>();
    }
}
