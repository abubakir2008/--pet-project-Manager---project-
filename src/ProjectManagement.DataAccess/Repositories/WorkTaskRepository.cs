using Microsoft.EntityFrameworkCore;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;

namespace ProjectManagement.DataAccess.Repositories;

public class WorkTaskRepository : IWorkTaskRepository
{
    private readonly ApplicationDbContext _db;

    public WorkTaskRepository(ApplicationDbContext db) => _db = db;

    public async Task<(IReadOnlyList<WorkTask> Items, int TotalCount)> GetPagedAsync(WorkTaskFilter filter, CancellationToken ct = default)
    {
        var query = _db.WorkTasks
            .AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .AsQueryable();

        // Role based visibility first, so that the total count matches what the user may see.
        if (filter.RestrictToProjectManagerId is not null)
            query = query.Where(t => t.Project.ManagerId == filter.RestrictToProjectManagerId);
        else if (filter.RestrictToAssigneeId is not null)
            query = query.Where(t => t.AssigneeId == filter.RestrictToAssigneeId);

        if (filter.ProjectId.HasValue)
            query = query.Where(t => t.ProjectId == filter.ProjectId.Value);
        if (filter.Status.HasValue)
            query = query.Where(t => t.Status == filter.Status.Value);
        if (!string.IsNullOrWhiteSpace(filter.AssigneeId))
            query = query.Where(t => t.AssigneeId == filter.AssigneeId);
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(t => t.Title.ToLower().Contains(search));
        }

        query = (filter.SortBy, filter.Descending) switch
        {
            (WorkTaskSortField.Title, true) => query.OrderByDescending(t => t.Title),
            (WorkTaskSortField.Title, false) => query.OrderBy(t => t.Title),
            (WorkTaskSortField.Status, true) => query.OrderByDescending(t => t.Status),
            (WorkTaskSortField.Status, false) => query.OrderBy(t => t.Status),
            (_, true) => query.OrderByDescending(t => t.Priority),
            (_, false) => query.OrderBy(t => t.Priority)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<WorkTask?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        _db.WorkTasks
            .Include(t => t.Project)
            .Include(t => t.Author)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<WorkTask?> GetAsync(int id, CancellationToken ct = default) =>
        _db.WorkTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public void Add(WorkTask task) => _db.WorkTasks.Add(task);

    public void Remove(WorkTask task) => _db.WorkTasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
