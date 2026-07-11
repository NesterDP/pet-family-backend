using Microsoft.AspNetCore.Mvc;
using PetFamily.Accounts.Application.Commands.CompleteUploadAvatar;
using PetFamily.Accounts.Application.Commands.ConfirmEmail;
using PetFamily.Accounts.Application.Commands.GenerateEmailToken;
using PetFamily.Accounts.Application.Commands.Login;
using PetFamily.Accounts.Application.Commands.Logout;
using PetFamily.Accounts.Application.Commands.RefreshTokens;
using PetFamily.Accounts.Application.Commands.Register;
using PetFamily.Accounts.Application.Commands.StartUploadAvatar;
using PetFamily.Accounts.Application.Queries.GetUserById;
using PetFamily.Accounts.Contracts.Requests;
using PetFamily.Accounts.Presentation.Accounts.Requests;
using PetFamily.Framework;
using PetFamily.Framework.Security.Authorization;
using PetFamily.SharedKernel.CustomErrors;

namespace PetFamily.Accounts.Presentation.Accounts;

public class AccountsController : ApplicationController
{
    private readonly UserScopedData _userData;

    public AccountsController(UserScopedData userData)
    {
        _userData = userData;
    }

    [Permission("accounts.GetUserInfoById")]
    [HttpPost("test")]
    public async Task<IActionResult> Test(
        CancellationToken cancellationToken)
    {
        return Ok("test");
    }

    [Permission("accounts.GetUserInfoById")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetUserInfoById(
        [FromRoute] Guid id,
        [FromServices] GetUserInfoByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var query = new GetUserInfoByIdQuery(id);
        var result = await handler.HandleAsync(query, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [HttpPost("email-confirmation")]
    public async Task<IActionResult> ConfirmEmail(
        [FromBody] ConfirmEmailRequest request,
        [FromServices] ConfirmEmailHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [HttpGet("email-confirmation-get")]
    public async Task<IActionResult> ConfirmEmailGet(
        [FromQuery] Guid userId,
        [FromQuery] string token,
        [FromServices] ConfirmEmailHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new ConfirmEmailCommand(userId, token);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [HttpPost("registration")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterUserRequest request,
        [FromServices] RegisterUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(
        [FromBody] LoginUserRequest request,
        [FromServices] LoginUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand();
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return result.Error.ToResponse();

        HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken.ToString());

        return result.ToResponse();
    }

    [HttpPost("logout")]

    public async Task<IActionResult> Logout(
        [FromServices] LogoutUserHandler handler)
    {
        var refreshTokenCookie = HttpContext.Request.Cookies["refreshToken"];
        if (!Guid.TryParse(refreshTokenCookie, out var refreshToken))
            return Errors.General.Failure("refreshToken has invalid format").ToResponse();

        var command = new LogoutUserCommand(refreshToken);
        var result = await handler.HandleAsync(command, CancellationToken.None);

        HttpContext.Response.Cookies.Delete("refreshToken");
        return result.ToResponse();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshTokens(
        [FromServices] RefreshTokensHandler handler,
        CancellationToken cancellationToken)
    {
        if (!HttpContext.Request.Cookies.TryGetValue("refreshToken", out var requestRefreshToken))
        {
            return Unauthorized();
        }

        var command = new RefreshTokensCommand(Guid.Parse(requestRefreshToken));
        var result = await handler.HandleAsync(command, cancellationToken);
        if (result.IsFailure)
            return result.Error.ToResponse();

        HttpContext.Response.Cookies.Append("refreshToken", result.Value.RefreshToken.ToString());
        return result.ToResponse();
    }

    [Permission("accounts.StartUploadAvatar")]
    [HttpPost("avatar-start")]
    public async Task<IActionResult> StartUploadAvatar(
        [FromBody] StartUploadAvatarRequest request,
        [FromServices] StartUploadAvatarHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(_userData.UserId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [Permission("accounts.CompleteUploadAvatar")]
    [HttpPost("avatar-complete")]
    public async Task<IActionResult> CompleteUploadAvatar(
        [FromBody] CompleteUploadAvatarRequest request,
        [FromServices] CompleteUploadAvatarHandler handler,
        CancellationToken cancellationToken)
    {
        var command = request.ToCommand(_userData.UserId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }

    [Permission("accounts.EmailTokenGeneration")]
    [HttpPost("email-token-generation")]
    public async Task<IActionResult> EmailTokenGeneration(
        [FromBody] GenerateEmailTokenRequest request,
        [FromServices] GenerateEmailTokenHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new GenerateEmailTokenCommand(request.UserId);
        var result = await handler.HandleAsync(command, cancellationToken);
        return result.IsFailure ? result.Error.ToResponse() : result.ToResponse();
    }
}