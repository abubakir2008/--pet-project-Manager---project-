using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.DataAccess.Repositories;

public interface IEmployeeRepository
{
    /// <summary>
    /// Partial search by first / last / middle name and e-mail, used by the AJAX autocomplete.
    /// </summary>
    Task<IReadOnlyList<Employee>> SearchAsync(string? search, int take, CancellationToken ct = default);

    Task<Employee?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>Returns the subset of the given ids that does not exist in the database.</summary>
    Task<List<string>> GetMissingIdsAsync(IReadOnlyCollection<string> ids, CancellationToken ct = default);

    /// <summary>True when the employee is set as the manager of at least one project.</summary>
    Task<bool> ManagesAnyProjectAsync(string employeeId, CancellationToken ct = default);

    /// <summary>True when the employee is the author or the assignee of at least one task.</summary>
    Task<bool> HasAnyTaskAsync(string employeeId, CancellationToken ct = default);
}
