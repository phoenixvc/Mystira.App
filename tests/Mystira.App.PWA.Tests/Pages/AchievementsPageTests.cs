using Bunit;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Mystira.App.PWA.Models;
using Mystira.App.PWA.Pages;
using Mystira.App.PWA.Services;
using Xunit;

namespace Mystira.App.PWA.Tests.Pages;

public class AchievementsPageTests : BunitContext
{
    [Fact]
    public void AchievementsPage_DisplaysTierCards()
    {
        Services.AddLogging();

        var account = new Account { Id = "acc-1", DisplayName = "Test" };
        var profiles = new List<UserProfile>
        {
            new() { Id = "p1", Name = "Ava", AgeGroup = "6-9" }
        };

        Services.AddSingleton<IAuthService>(new FakeAuthService(account));
        Services.AddSingleton<IProfileService>(new FakeProfileService(profiles));
        Services.AddSingleton<IPlayerContextService>(new FakePlayerContextService());
        Services.AddSingleton<IAchievementsService>(new FakeAchievementsService());

        var cut = Render<AchievementsPage>();

        // Switch to Advanced view where tier cards are rendered
        cut.WaitForAssertion(() => cut.Find(".view-toggle .btn-outline-secondary").Click());

        // Should display both earned and in-progress tiers
        cut.WaitForAssertion(() => cut.FindAll(".tier-card").Count.Should().Be(2));

        // Verify one is earned and one is locked (in-progress)
        var tierCards = cut.FindAll(".tier-card");
        tierCards.Count(c => c.ClassList.Contains("earned")).Should().Be(1);
        tierCards.Count(c => c.ClassList.Contains("locked")).Should().Be(1);
    }

    private sealed class FakeAuthService : IAuthService
    {
        private readonly Account _account;

        public FakeAuthService(Account account)
        {
            _account = account;
        }

        public Task<bool> IsAuthenticatedAsync() => Task.FromResult(true);
        public Task<Account?> GetCurrentAccountAsync() => Task.FromResult<Account?>(_account);
        public Task<string?> GetTokenAsync() => Task.FromResult<string?>(null);
        public Task<bool> LoginAsync(string email, string password) => Task.FromResult(false);
        public Task LogoutAsync() => Task.CompletedTask;
        public Task<(bool Success, string Message)> RequestPasswordlessSignupAsync(string email, string displayName) => Task.FromResult((false, string.Empty));
        public Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSignupAsync(string email, string code) => Task.FromResult((false, string.Empty, (Account?)null));
        public Task<(bool Success, string Message)> RequestPasswordlessSigninAsync(string email) => Task.FromResult((false, string.Empty));
        public Task<(bool Success, string Message, Account? Account)> VerifyPasswordlessSigninAsync(string email, string code) => Task.FromResult((false, string.Empty, (Account?)null));
        public Task<(bool Success, string Message, string? Token, string? RefreshToken)> RefreshTokenAsync(string token, string refreshToken) => Task.FromResult((false, string.Empty, (string?)null, (string?)null));
        public Task<string?> GetCurrentTokenAsync() => Task.FromResult<string?>(null);
        public void SetRememberMe(bool rememberMe) { }
        public Task<bool> GetRememberMeAsync() => Task.FromResult(false);
        public DateTime? GetTokenExpiryTime() => null;
        public Task<bool> EnsureTokenValidAsync(int expiryBufferMinutes = 5) => Task.FromResult(true);
#pragma warning disable CS0067 // Event is never used
        public event EventHandler? TokenExpiryWarning;
        public event EventHandler<bool>? AuthenticationStateChanged;
#pragma warning restore CS0067
    }

    private sealed class FakeProfileService : IProfileService
    {
        private readonly List<UserProfile> _profiles;

        public FakeProfileService(List<UserProfile> profiles)
        {
            _profiles = profiles;
        }

        public Task<List<UserProfile>?> GetUserProfilesAsync(string accountId) => Task.FromResult<List<UserProfile>?>(_profiles);
        public Task<bool> HasProfilesAsync(string accountId) => Task.FromResult(_profiles.Any());
        public Task<UserProfile?> CreateProfileAsync(CreateUserProfileRequest request) => Task.FromResult<UserProfile?>(null);
        public Task<UserProfile?> GetProfileAsync(string profileId) => Task.FromResult<UserProfile?>(_profiles.FirstOrDefault(p => p.Id == profileId));
        public Task<bool> DeleteProfileAsync(string profileId) => Task.FromResult(false);
        public Task<UserProfile?> UpdateProfileAsync(string profileId, UpdateUserProfileRequest request) => Task.FromResult<UserProfile?>(null);
    }

    private sealed class FakePlayerContextService : IPlayerContextService
    {
        private string? _selected;

        public Task<string?> GetSelectedProfileIdAsync() => Task.FromResult(_selected);

        public Task SetSelectedProfileIdAsync(string profileId)
        {
            _selected = profileId;
            return Task.CompletedTask;
        }

        public Task ClearSelectedProfileIdAsync()
        {
            _selected = null;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAchievementsService : IAchievementsService
    {
        public Task<AchievementsLoadResult> GetAchievementsAsync(UserProfile profile, CancellationToken cancellationToken = default)
        {
            var model = new AchievementsViewModel
            {
                ProfileId = profile.Id,
                ProfileName = profile.Name,
                AgeGroupId = profile.AgeGroup,
                Axes =
                {
                    new AxisAchievementsSectionViewModel
                    {
                        AxisId = "Courage",
                        AxisName = "Courage",
                        CurrentScore = 10,
                        Tiers =
                        {
                            new BadgeTierViewModel { BadgeId = "b1", Tier = "Bronze", TierOrder = 1, Title = "Bronze", Description = "", RequiredScore = 5, IsEarned = true, CurrentScore = 10 },
                            new BadgeTierViewModel { BadgeId = "b2", Tier = "Silver", TierOrder = 2, Title = "Silver", Description = "", RequiredScore = 15, IsEarned = false, CurrentScore = 10 }
                        }
                    }
                }
            };

            return Task.FromResult(AchievementsLoadResult.Success(model));
        }
    }
}
