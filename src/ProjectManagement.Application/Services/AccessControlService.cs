using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.DataAccess.Repositories;

namespace ProjectManagement.Application.Services;

/// <summary>
/// Implements the rules of the access control task:
/// director sees and manages everything; project manager works with the projects they
/// manage and the tasks of those projects; employee sees the projects they are assigned
/// to and may only change the status of their own tasks.
/// </summary>
public class AccessControlService : IAccessControlService
{
    private readonly IProjectRepository _projects;
    private readonly IWorkTaskRepository _tasks;

    public AccessControlService(IProjectRepository projects, IWorkTaskRepository tasks)
    {
        _projects = projects;
        _tasks = tasks;
    }

    public async Task<bool> CanViewProjectAsync(CurrentUser user, int projectId, CancellationToken ct = default)
    {
        if (user.IsDirector) return true;

        var project = await _projects.GetAsync(projectId, ct);
        if (project is null) return false;

        if (user.IsProjectManager && project.ManagerId == user.EmployeeId) return true;

        return await _projects.HasMemberAsync(projectId, user.EmployeeId, ct);
    }

    public async Task<bool> CanManageProjectAsync(CurrentUser user, int projectId, CancellationToken ct = default)
    {
        if (user.IsDirector) return true;
        if (!user.IsProjectManager) return false;

        var project = await _projects.GetAsync(projectId, ct);
        return project is not null && project.ManagerId == user.EmployeeId;
    }

    public async Task<bool> CanViewTaskAsync(CurrentUser user, int taskId, CancellationToken ct = default)
    {
        if (user.IsDirector) return true;

        var task = await _tasks.GetAsync(taskId, ct);
        if (task is null) return false;

        if (task.AssigneeId == user.EmployeeId || task.AuthorId == user.EmployeeId) return true;

        return await CanViewProjectAsync(user, task.ProjectId, ct);
    }

    public async Task<bool> CanManageTaskAsync(CurrentUser user, int taskId, CancellationToken ct = default)
    {
        if (user.IsDirector) return true;

        var task = await _tasks.GetAsync(taskId, ct);
        if (task is null) return false;

        return await CanManageProjectAsync(user, task.ProjectId, ct);
    }

    public async Task<bool> CanChangeTaskStatusAsync(CurrentUser user, int taskId, CancellationToken ct = default)
    {
        if (await CanManageTaskAsync(user, taskId, ct)) return true;

        // An employee may change the status of a task assigned to them.
        var task = await _tasks.GetAsync(taskId, ct);
        return task is not null && task.AssigneeId == user.EmployeeId;
    }
}
