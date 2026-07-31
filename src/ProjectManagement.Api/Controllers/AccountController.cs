using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Dtos;

namespace ProjectManagement.Api.Controllers;

[Route("api/account")]
public class AccountController : ApiControllerBase
{
    private readonly IAccountService _accounts;

    public AccountController(IAccountService accounts) => _accounts = accounts;

    /// <summary>Signs the employee in and returns a JWT.</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResultDto>> Login(LoginDto dto, CancellationToken ct) =>
        FromResult(await _accounts.LoginAsync(dto, ct));

    /// <summary>Returns the profile of the signed in employee.</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<LoginResultDto>> Me(CancellationToken ct) =>
        FromResult(await _accounts.GetCurrentAsync(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty, ct));
}
