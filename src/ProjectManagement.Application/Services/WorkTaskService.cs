using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;
using ProjectManagement.DataAccess.Repositories;

namespace ProjectManagement.Application.Services;

public class WorkTaskService : IWorkTaskService
{
    private const int MaxPageSize = 100;

    private readonly IWorkTaskRepository _tasks;
    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;
    private readonly IAccessControlService _access;

    public WorkTaskService(
        IWorkTaskRepository tasks,
        IProjectRepository projects,
        IEmployeeRepository employees,
        IAccessControlService access)
    {
        _tasks = tasks;
        _projects = projects;
        _employees = employees;
        _access = access;
    }

    public async Task<Result<PagedResult<WorkTaskDto>>> GetAllAsync(WorkTaskQueryParams query, CurrentUser user, CancellationToken ct = default)
    {
        var filter = BuildFilter(query, user);
        var (items, totalCount) = await _tasks.GetPagedAsync(filter, ct);

        return Result<PagedResult<WorkTaskDto>>.Success(new PagedResult<WorkTaskDto>
        {
            Items = items.Select(t => t.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<Result<WorkTaskDto>> GetByIdAsync(int id, CurrentUser user, CancellationToken ct = default)
    {
        var task = await _tasks.GetDetailsAsync(id, ct);
        if (task is null) return Result<WorkTaskDto>.NotFound("Task not found.");

        if (!await _access.CanViewTaskAsync(user, id, ct))
            return Result<WorkTaskDto>.Forbidden("You are not allowed to view this task.");

        return Result<WorkTaskDto>.Success(task.ToDto());
    }

    public async Task<Result<WorkTaskDto>> CreateAsync(WorkTaskCreateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(dto.ProjectId, ct))
            return Result<WorkTaskDto>.Validation("The selected project does not exist.");

        if (!await _access.CanManageProjectAsync(user, dto.ProjectId, ct))
            return Result<WorkTaskDto>.Forbidden("You are not allowed to add tasks to this project.");

        var assigneeCheck = await ValidateAssigneeAsync(dto.AssigneeId, dto.ProjectId, ct);
        if (assigneeCheck.IsFailure) return Result<WorkTaskDto>.Failure(assigneeCheck.ErrorType, assigneeCheck.Errors.ToArray());

        var task = new WorkTask
        {
            Title = dto.Title.Trim(),
            ProjectId = dto.ProjectId,
            AuthorId = user.EmployeeId,
            AssigneeId = string.IsNullOrWhiteSpace(dto.AssigneeId) ? null : dto.AssigneeId,
            Comment = dto.Comment,
            Priority = dto.Priority,
            Status = DataAccess.Entities.Enums.WorkTaskStatus.ToDo
        };

        _tasks.Add(task);
        await _tasks.SaveChangesAsync(ct);

        // Reload so that the response carries the project, author and assignee names.
        var created = await _tasks.GetDetailsAsync(task.Id, ct);
        return Result<WorkTaskDto>.Success(created!.ToDto());
    }

    public async Task<Result> UpdateAsync(int id, WorkTaskUpdateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        var task = await _tasks.GetAsync(id, ct);
        if (task is null) return Result.NotFound("Task not found.");

        if (!await _access.CanManageTaskAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this task.");

        var assigneeCheck = await ValidateAssigneeAsync(dto.AssigneeId, task.ProjectId, ct);
        if (assigneeCheck.IsFailure) return assigneeCheck;

        task.Title = dto.Title.Trim();
        task.AssigneeId = string.IsNullOrWhiteSpace(dto.AssigneeId) ? null : dto.AssigneeId;
        task.Status = dto.Status;
        task.Comment = dto.Comment;
        task.Priority = dto.Priority;

        await _tasks.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> ChangeStatusAsync(int id, WorkTaskStatusUpdateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        var task = await _tasks.GetAsync(id, ct);
        if (task is null) return Result.NotFound("Task not found.");

        if (!Enum.IsDefined(typeof(DataAccess.Entities.Enums.WorkTaskStatus), dto.Status))
            return Result.Validation("Unknown task status.");

        if (!await _access.CanChangeTaskStatusAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to change the status of this task.");

        task.Status = dto.Status;
        await _tasks.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CurrentUser user, CancellationToken ct = default)
    {
        var task = await _tasks.GetAsync(id, ct);
        if (task is null) return Result.NotFound("Task not found.");

        if (!await _access.CanManageTaskAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to delete this task.");

        _tasks.Remove(task);
        await _tasks.SaveChangesAsync(ct);
        return Result.Success();
    }

    private static WorkTaskFilter BuildFilter(WorkTaskQueryParams query, CurrentUser user)
    {
        var filter = new WorkTaskFilter
        {
            ProjectId = query.ProjectId,
            Status = query.Status,
            AssigneeId = query.AssigneeId,
            Search = query.Search,
            SortBy = ParseSortField(query.SortBy),
            Descending = query.Desc,
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize)
        };

        if (!user.IsDirector)
        {
            if (user.IsProjectManager) filter.RestrictToProjectManagerId = user.EmployeeId;
            else filter.RestrictToAssigneeId = user.EmployeeId;
        }

        return filter;
    }

    private static WorkTaskSortField ParseSortField(string? sortBy) => sortBy?.Trim().ToLowerInvariant() switch
    {
        "title" => WorkTaskSortField.Title,
        "status" => WorkTaskSortField.Status,
        _ => WorkTaskSortField.Priority
    };

    /// <summary>
    /// The specification allows only project executors to be assigned to a task,
    /// so an unknown or unrelated employee is reported as a validation error.
    /// </summary>
    private async Task<Result> ValidateAssigneeAsync(string? assigneeId, int projectId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(assigneeId)) return Result.Success();

        if (await _employees.GetAsync(assigneeId, ct) is null)
            return Result.Validation("The selected assignee does not exist.");

        if (!await _projects.HasMemberAsync(projectId, assigneeId, ct))
            return Result.Validation("The assignee must be one of the project executors.");

        return Result.Success();
    }
}
