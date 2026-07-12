using PetFamily.Core.Abstractions;

namespace PetFamily.Accounts.Application.Commands.Logout;

public record LogoutUserCommand(Guid RefreshToken) : ICommand;