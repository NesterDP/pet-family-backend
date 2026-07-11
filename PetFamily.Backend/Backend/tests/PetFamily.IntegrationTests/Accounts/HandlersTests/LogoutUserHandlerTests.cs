using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using PetFamily.Accounts.Application.Commands.Logout;
using PetFamily.Accounts.Contracts.Responses;
using PetFamily.Accounts.Domain.DataModels;
using PetFamily.Core.Abstractions;
using PetFamily.IntegrationTests.Accounts.Heritage;
using PetFamily.IntegrationTests.General;
using PetFamily.SharedKernel.Constants;

namespace PetFamily.IntegrationTests.Accounts.HandlersTests;

public class LogoutUserHandlerTests : AccountsTestsBase
{
    private readonly ICommandHandler<LogoutResponse, LogoutUserCommand> _sut;

    public LogoutUserHandlerTests(AccountsTestsWebFactory factory)
        : base(factory)
    {
        _sut = Scope.ServiceProvider.GetRequiredService<ICommandHandler<LogoutResponse, LogoutUserCommand>>();
    }

    [Fact]
    public async Task LogoutUser_success_should_delete_refresh_token_from_cache()
    {
        // arrange
        const string EMAIL = "test@mail.com";
        const string USERNAME = "testUserName";
        const string PASSWORD = "Password121314s.";

        var user = await DataGenerator.SeedUserAsync(USERNAME, EMAIL, PASSWORD, UserManager, RoleManager);
        var accessToken = await TokenProvider.GenerateAccessToken(user, CancellationToken.None);
        var refreshToken = await TokenProvider.GenerateRefreshToken(user, accessToken.Jti, CancellationToken.None);

        var command = new LogoutUserCommand(refreshToken);

        // act
        var result = await _sut.HandleAsync(command, CancellationToken.None);

        // assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ResponseMessage.Should().NotBeNullOrEmpty();

        // refresh session was deleted from cache
        string key = CacheConstants.REFRESH_SESSIONS_PREFIX + refreshToken;
        var refreshSession = await CacheService.GetAsync<RefreshSession>(key, CancellationToken.None);
        refreshSession.Should().BeNull();
    }
}
