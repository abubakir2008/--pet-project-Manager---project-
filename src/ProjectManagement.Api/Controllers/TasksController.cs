using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Api.Controllers;

[Route("api/tasks")]
[Authorize]
public class TasksController : ApiControllerBase
{
    private readonly IWorkTaskService _tasks;

    public TasksController(IWorkTaskService tasks) => _tasks = tasks;

    /// <summary>Task list with filtering (project, status, assignee, text) and sorting.</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkTaskDto>>> GetAll([FromQuery] WorkTaskQueryParams query, CancellationToken ct) =>
        FromResult(await _tasks.GetAllAsync(query, CurrentUser, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkTaskDto>> GetById(int id, CancellationToken ct) =>
        FromResult(await _tasks.GetByIdAsync(id, CurrentUser, ct));

    /// <summary>Creates a task inside a project; ProjectId is required by the DTO.</summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<ActionResult<WorkTaskDto>> Create(WorkTaskCreateDto dto, CancellationToken ct)
    {
        var result = await _tasks.CreateAsync(dto, CurrentUser, ct);
        return FromCreatedResult(result, nameof(GetById), new { id = result.Value?.Id });
    }

    /// <summary>Full edit: title, assignee, status, comment and priority.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> Update(int id, WorkTaskUpdateDto dto, CancellationToken ct) =>
        FromResult(await _tasks.UpdateAsync(id, dto, CurrentUser, ct));

    /// <summary>Status change; also available to the employee the task is assigned to.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> ChangeStatus(int id, WorkTaskStatusUpdateDto dto, CancellationToken ct) =>
        FromResult(await _tasks.ChangeStatusAsync(id, dto, CurrentUser, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        FromResult(await _tasks.DeleteAsync(id, CurrentUser, ct));
}
