using Microsoft.EntityFrameworkCore;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;

namespace ProjectManagement.DataAccess.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _db;

    public ProjectRepository(ApplicationDbContext db) => _db = db;

    public async Task<(IReadOnlyList<ProjectListRow> Items, int TotalCount)> GetPagedAsync(ProjectFilter filter, CancellationToken ct = default)
    {
        var query = _db.Projects.AsNoTracking();

        // Role based visibility is applied first so that the total count matches what the user may see.
        if (filter.RestrictToManagerId is not null)
            query = query.Where(p => p.ManagerId == filter.RestrictToManagerId);
        else if (filter.RestrictToMemberId is not null)
            query = query.Where(p => p.ProjectEmployees.Any(pe => pe.EmployeeId == filter.RestrictToMemberId));

        if (filter.StartDateFrom.HasValue)
            query = query.Where(p => p.StartDate >= filter.StartDateFrom.Value);
        if (filter.StartDateTo.HasValue)
            query = query.Where(p => p.StartDate <= filter.StartDateTo.Value);
        if (filter.PriorityFrom.HasValue)
            query = query.Where(p => p.Priority >= filter.PriorityFrom.Value);
        if (filter.PriorityTo.HasValue)
            query = query.Where(p => p.Priority <= filter.PriorityTo.Value);
        if (!string.IsNullOrWhiteSpace(filter.ManagerId))
            query = query.Where(p => p.ManagerId == filter.ManagerId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search)
                || p.CustomerCompany.ToLower().Contains(search)
                || p.ContractorCompany.ToLower().Contains(search));
        }

        query = (filter.SortBy, filter.Descending) switch
        {
            (ProjectSortField.Name, true) => query.OrderByDescending(p => p.Name),
            (ProjectSortField.Name, false) => query.OrderBy(p => p.Name),
            (ProjectSortField.EndDate, true) => query.OrderByDescending(p => p.EndDate),
            (ProjectSortField.EndDate, false) => query.OrderBy(p => p.EndDate),
            (ProjectSortField.Priority, true) => query.OrderByDescending(p => p.Priority),
            (ProjectSortField.Priority, false) => query.OrderBy(p => p.Priority),
            (_, true) => query.OrderByDescending(p => p.StartDate),
            (_, false) => query.OrderBy(p => p.StartDate)
        };

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            // Counts are computed in SQL instead of loading the whole collections.
            .Select(p => new ProjectListRow
            {
                Id = p.Id,
                Name = p.Name,
                CustomerCompany = p.CustomerCompany,
                ContractorCompany = p.ContractorCompany,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Priority = p.Priority,
                ManagerId = p.ManagerId,
                ManagerFirstName = p.Manager != null ? p.Manager.FirstName : null,
                ManagerLastName = p.Manager != null ? p.Manager.LastName : null,
                ManagerMiddleName = p.Manager != null ? p.Manager.MiddleName : null,
                EmployeeCount = p.ProjectEmployees.Count,
                TaskCount = p.Tasks.Count
            })
            .ToListAsync(ct);

        return (items, totalCount);
    }

    public Task<Project?> GetDetailsAsync(int id, CancellationToken ct = default) =>
        _db.Projects
            .Include(p => p.Manager)
            .Include(p => p.ProjectEmployees).ThenInclude(pe => pe.Employee)
            .Include(p => p.Tasks)
            .Include(p => p.Documents)
            .FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Project?> GetAsync(int id, CancellationToken ct = default) =>
        _db.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<bool> ExistsAsync(int id, CancellationToken ct = default) =>
        _db.Projects.AnyAsync(p => p.Id == id, ct);

    public Task<List<string>> GetMemberIdsAsync(int projectId, CancellationToken ct = default) =>
        _db.ProjectEmployees
            .Where(pe => pe.ProjectId == projectId)
            .Select(pe => pe.EmployeeId)
            .ToListAsync(ct);

    public void Add(Project project) => _db.Projects.Add(project);

    public void Remove(Project project) => _db.Projects.Remove(project);

    public Task AddMemberAsync(int projectId, string employeeId, CancellationToken ct = default)
    {
        _db.ProjectEmployees.Add(new ProjectEmployee { ProjectId = projectId, EmployeeId = employeeId });
        return Task.CompletedTask;
    }

    public async Task<bool> RemoveMemberAsync(int projectId, string employeeId, CancellationToken ct = default)
    {
        var link = await _db.ProjectEmployees
            .FirstOrDefaultAsync(pe => pe.ProjectId == projectId && pe.EmployeeId == employeeId, ct);
        if (link is null) return false;

        _db.ProjectEmployees.Remove(link);
        return true;
    }

    public Task<bool> HasMemberAsync(int projectId, string employeeId, CancellationToken ct = default) =>
        _db.ProjectEmployees.AnyAsync(pe => pe.ProjectId == projectId && pe.EmployeeId == employeeId, ct);

    public void AddDocument(ProjectDocument document) => _db.ProjectDocuments.Add(document);

    public void RemoveDocument(ProjectDocument document) => _db.ProjectDocuments.Remove(document);

    public Task<ProjectDocument?> GetDocumentAsync(int projectId, int documentId, CancellationToken ct = default) =>
        _db.ProjectDocuments.FirstOrDefaultAsync(d => d.Id == documentId && d.ProjectId == projectId, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
