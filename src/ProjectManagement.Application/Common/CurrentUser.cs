using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Application.Common;

/// <summary>
/// The signed in employee as seen by the logic layer. Built by the presentation layer
/// from the JWT claims, so the services do not depend on ClaimsPrincipal.
/// </summary>
public sealed class CurrentUser
{
    public CurrentUser(string employeeId, IReadOnlyCollection<string> roles)
    {
        EmployeeId = employeeId;
        Roles = roles;
    }

    public string EmployeeId { get; }
    public IReadOnlyCollection<string> Roles { get; }

    public bool IsDirector => Roles.Contains(AppRoles.Director);
    public bool IsProjectManager => Roles.Contains(AppRoles.ProjectManager);
    public bool IsEmployee => Roles.Contains(AppRoles.Employee);
}
