using Microsoft.AspNetCore.Identity;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Repositories;

namespace ProjectManagement.Application.Services;

public class EmployeeService : IEmployeeService
{
    private const int MaxTake = 100;

    private readonly UserManager<Employee> _userManager;
    private readonly IEmployeeRepository _employees;

    public EmployeeService(UserManager<Employee> userManager, IEmployeeRepository employees)
    {
        _userManager = userManager;
        _employees = employees;
    }

    public async Task<Result<List<EmployeeDto>>> SearchAsync(EmployeeQueryParams query, CancellationToken ct = default)
    {
        // The page size comes from the client, so it is clamped to keep the query bounded.
        var take = Math.Clamp(query.Take, 1, MaxTake);
        var employees = await _employees.SearchAsync(query.Search, take, ct);

        var result = new List<EmployeeDto>(employees.Count);
        foreach (var employee in employees)
            result.Add(employee.ToDto(await _userManager.GetRolesAsync(employee)));

        return Result<List<EmployeeDto>>.Success(result);
    }

    public async Task<Result<EmployeeDto>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var employee = await _employees.GetAsync(id, ct);
        if (employee is null) return Result<EmployeeDto>.NotFound("Employee not found.");

        return Result<EmployeeDto>.Success(employee.ToDto(await _userManager.GetRolesAsync(employee)));
    }

    public async Task<Result<EmployeeDto>> CreateAsync(EmployeeCreateDto dto, CancellationToken ct = default)
    {
        var email = dto.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) is not null)
            return Result<EmployeeDto>.Conflict("An employee with this e-mail already exists.");

        if (!AppRoles.All.Contains(dto.Role))
            return Result<EmployeeDto>.Validation($"Unknown role '{dto.Role}'.");

        var employee = new Employee
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            FirstName = dto.FirstName.Trim(),
            LastName = dto.LastName.Trim(),
            MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim()
        };

        var created = await _userManager.CreateAsync(employee, dto.Password);
        if (!created.Succeeded)
            return Result<EmployeeDto>.Validation(created.Errors.Select(e => e.Description).ToArray());

        var roleAssigned = await _userManager.AddToRoleAsync(employee, dto.Role);
        if (!roleAssigned.Succeeded)
        {
            // Keep the store consistent: an account without a role could not be used anyway.
            await _userManager.DeleteAsync(employee);
            return Result<EmployeeDto>.Validation(roleAssigned.Errors.Select(e => e.Description).ToArray());
        }

        return Result<EmployeeDto>.Success(employee.ToDto(new List<string> { dto.Role }));
    }

    public async Task<Result> UpdateAsync(string id, EmployeeUpdateDto dto, CancellationToken ct = default)
    {
        var employee = await _userManager.FindByIdAsync(id);
        if (employee is null) return Result.NotFound("Employee not found.");

        var email = dto.Email.Trim();

        var owner = await _userManager.FindByEmailAsync(email);
        if (owner is not null && owner.Id != employee.Id)
            return Result.Conflict("Another employee already uses this e-mail.");

        if (!string.IsNullOrWhiteSpace(dto.Role) && !AppRoles.All.Contains(dto.Role))
            return Result.Validation($"Unknown role '{dto.Role}'.");

        employee.FirstName = dto.FirstName.Trim();
        employee.LastName = dto.LastName.Trim();
        employee.MiddleName = string.IsNullOrWhiteSpace(dto.MiddleName) ? null : dto.MiddleName.Trim();
        employee.Email = email;
        employee.UserName = email;

        var updated = await _userManager.UpdateAsync(employee);
        if (!updated.Succeeded)
            return Result.Validation(updated.Errors.Select(e => e.Description).ToArray());

        if (!string.IsNullOrWhiteSpace(dto.Role))
        {
            var currentRoles = await _userManager.GetRolesAsync(employee);
            if (!currentRoles.Contains(dto.Role))
            {
                await _userManager.RemoveFromRolesAsync(employee, currentRoles);
                await _userManager.AddToRoleAsync(employee, dto.Role);
            }
        }

        return Result.Success();
    }

    public async Task<Result> DeleteAsync(string id, CancellationToken ct = default)
    {
        var employee = await _userManager.FindByIdAsync(id);
        if (employee is null) return Result.NotFound("Employee not found.");

        // These references are restricted in the model; report them instead of
        // letting the database raise a foreign key violation.
        if (await _employees.ManagesAnyProjectAsync(id, ct))
            return Result.Conflict("This employee manages a project. Assign another manager first.");

        if (await _employees.HasAnyTaskAsync(id, ct))
            return Result.Conflict("This employee is the author or the assignee of a task. Reassign the tasks first.");

        var deleted = await _userManager.DeleteAsync(employee);
        if (!deleted.Succeeded)
            return Result.Validation(deleted.Errors.Select(e => e.Description).ToArray());

        return Result.Success();
    }
}
