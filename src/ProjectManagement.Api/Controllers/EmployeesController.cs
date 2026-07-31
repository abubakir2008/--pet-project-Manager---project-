using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Api.Controllers;

[Route("api/employees")]
[Authorize]
public class EmployeesController : ApiControllerBase
{
    private readonly IEmployeeService _employees;

    public EmployeesController(IEmployeeService employees) => _employees = employees;

    /// <summary>
    /// Employee list with partial search; used by the autocomplete on the wizard steps
    /// where a manager or the executors are picked.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<EmployeeDto>>> GetAll([FromQuery] EmployeeQueryParams query, CancellationToken ct) =>
        FromResult(await _employees.SearchAsync(query, ct));

    [HttpGet("{id}")]
    public async Task<ActionResult<EmployeeDto>> GetById(string id, CancellationToken ct) =>
        FromResult(await _employees.GetByIdAsync(id, ct));

    /// <summary>Only a director may add employees to the system.</summary>
    [HttpPost]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<ActionResult<EmployeeDto>> Create(EmployeeCreateDto dto, CancellationToken ct)
    {
        var result = await _employees.CreateAsync(dto, ct);
        return FromCreatedResult(result, nameof(GetById), new { id = result.Value?.Id });
    }

    [HttpPut("{id}")]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<IActionResult> Update(string id, EmployeeUpdateDto dto, CancellationToken ct) =>
        FromResult(await _employees.UpdateAsync(id, dto, ct));

    [HttpDelete("{id}")]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct) =>
        FromResult(await _employees.DeleteAsync(id, ct));
}
