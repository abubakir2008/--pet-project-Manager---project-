using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;

namespace ProjectManagement.DataAccess.Repositories;

public interface IWorkTaskRepository
{
    Task<(IReadOnlyList<WorkTask> Items, int TotalCount)> GetPagedAsync(WorkTaskFilter filter, CancellationToken ct = default);

    /// <summary>Loads a task with its project, author and assignee.</summary>
    Task<WorkTask?> GetDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Loads a task without related data; used for updates and permission checks.</summary>
    Task<WorkTask?> GetAsync(int id, CancellationToken ct = default);

    void Add(WorkTask task);
    void Remove(WorkTask task);

    Task SaveChangesAsync(CancellationToken ct = default);
}
