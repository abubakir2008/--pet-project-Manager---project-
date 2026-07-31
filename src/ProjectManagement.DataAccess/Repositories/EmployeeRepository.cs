using Microsoft.EntityFrameworkCore;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.DataAccess.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly ApplicationDbContext _db;

    public EmployeeRepository(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<Employee>> SearchAsync(string? search, int take, CancellationToken ct = default)
    {
        var query = _db.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(e =>
                e.FirstName.ToLower().Contains(term) ||
                e.LastName.ToLower().Contains(term) ||
                (e.MiddleName != null && e.MiddleName.ToLower().Contains(term)) ||
                (e.Email != null && e.Email.ToLower().Contains(term)));
        }

        return await query
            .OrderBy(e => e.LastName)
            .ThenBy(e => e.FirstName)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<Employee?> GetAsync(string id, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(e => e.Id == id, ct);

    public async Task<List<string>> GetMissingIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0) return new List<string>();

        var existing = await _db.Users
            .AsNoTracking()
            .Where(e => ids.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync(ct);

        return ids.Except(existing).ToList();
    }

    public Task<bool> ManagesAnyProjectAsync(string employeeId, CancellationToken ct = default) =>
        _db.Projects.AnyAsync(p => p.ManagerId == employeeId, ct);

    public Task<bool> HasAnyTaskAsync(string employeeId, CancellationToken ct = default) =>
        _db.WorkTasks.AnyAsync(t => t.AuthorId == employeeId || t.AssigneeId == employeeId, ct);
}
