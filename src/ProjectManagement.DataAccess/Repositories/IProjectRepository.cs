using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;

namespace ProjectManagement.DataAccess.Repositories;

public interface IProjectRepository
{
    /// <summary>Returns one page of projects together with the total number of matches.</summary>
    Task<(IReadOnlyList<ProjectListRow> Items, int TotalCount)> GetPagedAsync(ProjectFilter filter, CancellationToken ct = default);

    /// <summary>Loads a project with its manager, employees, tasks and documents.</summary>
    Task<Project?> GetDetailsAsync(int id, CancellationToken ct = default);

    /// <summary>Loads a project without related data; used for updates and permission checks.</summary>
    Task<Project?> GetAsync(int id, CancellationToken ct = default);

    Task<bool> ExistsAsync(int id, CancellationToken ct = default);

    /// <summary>Ids of the employees assigned to the project.</summary>
    Task<List<string>> GetMemberIdsAsync(int projectId, CancellationToken ct = default);

    void Add(Project project);
    void Remove(Project project);

    Task AddMemberAsync(int projectId, string employeeId, CancellationToken ct = default);
    Task<bool> RemoveMemberAsync(int projectId, string employeeId, CancellationToken ct = default);
    Task<bool> HasMemberAsync(int projectId, string employeeId, CancellationToken ct = default);

    void AddDocument(ProjectDocument document);
    void RemoveDocument(ProjectDocument document);
    Task<ProjectDocument?> GetDocumentAsync(int projectId, int documentId, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
