using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using PetFamily.Accounts.Application.Commands.RefreshTokens;
using PetFamily.Accounts.Contracts.Responses;
using PetFamily.Accounts.Domain.DataModels;
using PetFamily.Core;
using PetFamily.Core.Abstractions;
using PetFamily.IntegrationTests.Accounts.Heritage;
using PetFamily.IntegrationTests.General;
using PetFamily.SharedKernel.Constants;

namespace PetFamily.IntegrationTests.Accounts.HandlersTests;

public class RefreshTokensHandlerTests : AccountsTestsBase
{
    private readonly ICommandHandler<LoginResponse, RefreshTokensCommand> _sut;

    public RefreshTokensHandlerTests(AccountsTestsWebFactory factory)
        : base(factory)
    {
        _sut = Scope.ServiceProvider.GetRequiredService<ICommandHandler<LoginResponse, RefreshTokensCommand>>();
    }

    [Fact]
    public async Task RefreshTokens_success_should_return_new_access_and_refresh_tokens()
    {
        // arrange
        const string EMAIL = "test@mail.com";
        const string USERNAME = "testUserName";
        const string PASSWORD = "Password121314s.";

        var user = await DataGenerator.SeedUserAsync(USERNAME, EMAIL, PASSWORD, UserManager, RoleManager);
        var accessToken = await TokenProvider.GenerateAccessToken(user, CancellationToken.None);
        var refreshToken = await TokenProvider.GenerateRefreshToken(user, accessToken.Jti, CancellationToken.None);

        var command = new RefreshTokensCommand(refreshToken);

        // act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();

        // returned tokens should not be the same as they were before handler was executed
        result.Value.AccessToken.Should().NotBeEquivalentTo(accessToken.AccessToken);
        result.Value.RefreshToken.Should().NotBe(refreshToken);

        // refresh session was filled with data returned from handler
        var userClaims = await TokenProvider.GetUserClaims(result.Value.AccessToken);
        string userJtiString = userClaims.Value.FirstOrDefault(c => c.Type == CustomClaims.JTI)!.Value;

        string key = CacheConstants.REFRESH_SESSIONS_PREFIX + result.Value.RefreshToken;
        var refreshSession = await CacheService.GetAsync<RefreshSession>(key, CancellationToken.None);

        refreshSession!.UserId.Should().Be(user.Id);
        refreshSession.Jti.Should().Be(userJtiString);
        refreshSession.RefreshToken.Should().Be(result.Value.RefreshToken);
    }

    [Fact]
    public async Task RefreshTokens_failure_should_return_error_because_refresh_token_lifetime_is_expired()
    {
        // arrange
        const string EMAIL = "test@mail.com";
        const string USERNAME = "testUserName";
        const string PASSWORD = "Password121314s.";
        const int SECONDS_BEFORE_EXPIRATION = 1;

        var user = await DataGenerator.SeedUserAsync(USERNAME, EMAIL, PASSWORD, UserManager, RoleManager);
        var accessToken = await TokenProvider.GenerateAccessToken(user, CancellationToken.None);

        // creating session with expired lifetime
        var refreshToken = await GenerateRefreshToken(
            user,
            accessToken.Jti,
            DateTime.UtcNow.AddSeconds(SECONDS_BEFORE_EXPIRATION),
            CancellationToken.None);

        await Task.Delay(SECONDS_BEFORE_EXPIRATION);

        var command = new RefreshTokensCommand(refreshToken);

        // act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // assert
        result.IsFailure.Should().BeTrue();
    }

    private async Task<Guid> GenerateRefreshToken(
        User user,
        Guid accessTokenJti,
        DateTime expiresIn,
        CancellationToken cancellationToken)
    {
        var refreshSession = new RefreshSession
        {
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
            ExpiresIn = expiresIn,
            Jti = accessTokenJti,
            RefreshToken = Guid.NewGuid()
        };

        DistributedCacheEntryOptions options = new() { AbsoluteExpiration = expiresIn };

        string key = CacheConstants.REFRESH_SESSIONS_PREFIX + refreshSession.RefreshToken;
        await CacheService.SetAsync(key, refreshSession, options, cancellationToken);

        return refreshSession.RefreshToken;
    }
}