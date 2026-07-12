using CSharpFunctionalExtensions;
using Microsoft.Extensions.Logging;
using PetFamily.Accounts.Application.Abstractions;
using PetFamily.Accounts.Contracts.Responses;
using PetFamily.Core.Abstractions;
using PetFamily.SharedKernel.CustomErrors;

namespace PetFamily.Accounts.Application.Commands.Logout;

public class LogoutUserHandler : ICommandHandler<LogoutResponse, LogoutUserCommand>
{
    private readonly IRefreshSessionManager _refreshSessionManager;
    private readonly ILogger<LogoutUserHandler> _logger;

    public LogoutUserHandler(
        IRefreshSessionManager refreshSessionManager,
        ILogger<LogoutUserHandler> logger)
    {
        _refreshSessionManager = refreshSessionManager;
        _logger = logger;
    }

    public async Task<Result<LogoutResponse, ErrorList>> HandleAsync(
        LogoutUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var refreshSession = await _refreshSessionManager
            .GetByRefreshToken(command.RefreshToken, cancellationToken);

        if (refreshSession.IsFailure)
            return refreshSession.Error.ToErrorList();

        await _refreshSessionManager.DeleteAsync(refreshSession.Value, cancellationToken);

        _logger.LogInformation("Refresh token {RefreshToken} was successfully deleted", command.RefreshToken);

        return new LogoutResponse($"Refresh token for user with Id = {refreshSession.Value.UserId} " +
                                  $"was successfully deleted");
    }
}