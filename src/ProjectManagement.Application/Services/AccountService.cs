using Microsoft.AspNetCore.Identity;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Application.Services;

public class AccountService : IAccountService
{
    private readonly UserManager<Employee> _userManager;
    private readonly ITokenService _tokenService;

    public AccountService(UserManager<Employee> userManager, ITokenService tokenService)
    {
        _userManager = userManager;
        _tokenService = tokenService;
    }

    public async Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var employee = await _userManager.FindByEmailAsync(dto.Email.Trim());

        // The same message is returned for an unknown e-mail and a wrong password
        // so that the endpoint cannot be used to discover existing accounts.
        const string invalidCredentials = "Invalid e-mail or password.";

        if (employee is null)
            return Result<LoginResultDto>.Failure(ErrorType.Forbidden, invalidCredentials);

        if (await _userManager.IsLockedOutAsync(employee))
            return Result<LoginResultDto>.Failure(ErrorType.Forbidden, "The account is temporarily locked. Try again later.");

        if (!await _userManager.CheckPasswordAsync(employee, dto.Password))
        {
            await _userManager.AccessFailedAsync(employee);
            return Result<LoginResultDto>.Failure(ErrorType.Forbidden, invalidCredentials);
        }

        await _userManager.ResetAccessFailedCountAsync(employee);

        var roles = await _userManager.GetRolesAsync(employee);
        var (token, expiresAt) = _tokenService.CreateToken(employee, roles);

        return Result<LoginResultDto>.Success(new LoginResultDto
        {
            Token = token,
            ExpiresAt = expiresAt,
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }

    public async Task<Result<LoginResultDto>> GetCurrentAsync(string employeeId, CancellationToken ct = default)
    {
        var employee = await _userManager.FindByIdAsync(employeeId);
        if (employee is null) return Result<LoginResultDto>.NotFound("Account not found.");

        var roles = await _userManager.GetRolesAsync(employee);

        return Result<LoginResultDto>.Success(new LoginResultDto
        {
            EmployeeId = employee.Id,
            FullName = employee.FullName,
            Role = roles.FirstOrDefault() ?? string.Empty
        });
    }
}
