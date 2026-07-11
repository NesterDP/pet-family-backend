using PetFamily.Core.Abstractions;

namespace PetFamily.Accounts.Application.Commands.RefreshTokens;

public record RefreshTokensCommand(Guid RefreshToken) : ICommand;