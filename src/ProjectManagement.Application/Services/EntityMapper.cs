using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;

namespace ProjectManagement.Application.Services;

/// <summary>Hand written mapping between storage entities and the DTOs of the logic layer.</summary>
internal static class EntityMapper
{
    public static string BuildFullName(string? lastName, string? firstName, string? middleName) =>
        $"{lastName} {firstName} {middleName}".Trim();

    public static EmployeeDto ToDto(this Employee employee, IList<string>? roles = null) => new()
    {
        Id = employee.Id,
        FirstName = employee.FirstName,
        LastName = employee.LastName,
        MiddleName = employee.MiddleName,
        Email = employee.Email ?? string.Empty,
        Roles = roles ?? new List<string>()
    };

    public static ProjectListItemDto ToDto(this ProjectListRow row) => new()
    {
        Id = row.Id,
        Name = row.Name,
        CustomerCompany = row.CustomerCompany,
        ContractorCompany = row.ContractorCompany,
        StartDate = row.StartDate,
        EndDate = row.EndDate,
        Priority = row.Priority,
        ManagerId = row.ManagerId,
        ManagerFullName = BuildFullName(row.ManagerLastName, row.ManagerFirstName, row.ManagerMiddleName),
        EmployeeCount = row.EmployeeCount,
        TaskCount = row.TaskCount
    };

    public static WorkTaskDto ToDto(this WorkTask task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        ProjectId = task.ProjectId,
        ProjectName = task.Project?.Name ?? string.Empty,
        AuthorId = task.AuthorId,
        AuthorFullName = task.Author?.FullName ?? string.Empty,
        AssigneeId = task.AssigneeId,
        AssigneeFullName = task.Assignee?.FullName,
        Status = task.Status,
        Comment = task.Comment,
        Priority = task.Priority
    };
}
