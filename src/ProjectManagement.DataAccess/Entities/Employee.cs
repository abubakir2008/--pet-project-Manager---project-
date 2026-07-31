using Microsoft.AspNetCore.Identity;

namespace ProjectManagement.DataAccess.Entities;

/// <summary>
/// An employee is also the user account (ASP.NET Core Identity): employees sign in
/// and act under one of the application roles, so the two concepts are merged
/// instead of being kept in two tables that would have to be synchronised.
/// </summary>
public class Employee : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    public ICollection<ProjectEmployee> ProjectEmployees { get; set; } = new List<ProjectEmployee>();
    public ICollection<Project> ManagedProjects { get; set; } = new List<Project>();
    public ICollection<WorkTask> AuthoredTasks { get; set; } = new List<WorkTask>();
    public ICollection<WorkTask> AssignedTasks { get; set; } = new List<WorkTask>();

    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
}
