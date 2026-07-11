using CSharpFunctionalExtensions;
using Microsoft.AspNetCore.Identity;
using PetFamily.Accounts.Application.Abstractions;
using PetFamily.Accounts.Contracts.Responses;
using PetFamily.Accounts.Domain.DataModels;
using PetFamily.Core.Abstractions;
using PetFamily.SharedKernel.CustomErrors;

namespace PetFamily.Accounts.Application.Commands.RefreshTokens;

public class RefreshTokensHandler : ICommandHandler<LoginResponse, RefreshTokensCommand>
{
    private readonly IRefreshSessionManager _refreshSessionManager;
    private readonly ITokenProvider _tokenProvider;
    private readonly UserManager<User> _userManager;

    public RefreshTokensHandler(
        IRefreshSessionManager refreshSessionManager,
        ITokenProvider tokenProvider,
        UserManager<User> userManager)
    {
        _refreshSessionManager = refreshSessionManager;
        _tokenProvider = tokenProvider;
        _userManager = userManager;
    }

    public async Task<Result<LoginResponse, ErrorList>> HandleAsync(
        RefreshTokensCommand command,
        CancellationToken cancellationToken = default)
    {
        var oldRefreshSession = await _refreshSessionManager
            .GetByRefreshToken(command.RefreshToken, cancellationToken);

        if (oldRefreshSession.IsFailure)
            return oldRefreshSession.Error.ToErrorList();

        await _refreshSessionManager.DeleteAsync(oldRefreshSession.Value, cancellationToken);

        var user = await _userManager.FindByIdAsync(oldRefreshSession.Value.UserId.ToString());

        var newAccessToken = await _tokenProvider
            .GenerateAccessToken(user!, cancellationToken);

        var newRefreshToken = await _tokenProvider
            .GenerateRefreshToken(user!, newAccessToken.Jti, cancellationToken);

        return new LoginResponse(newAccessToken.AccessToken, newRefreshToken, user!.Id, user.Email!);
    }
}