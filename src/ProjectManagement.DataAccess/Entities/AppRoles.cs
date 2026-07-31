namespace ProjectManagement.DataAccess.Entities;

/// <summary>The three application roles required by the access control task.</summary>
public static class AppRoles
{
    /// <summary>Director: sees every page and manages every entity.</summary>
    public const string Director = "Director";

    /// <summary>Project manager: own projects and their tasks, cannot create employees.</summary>
    public const string ProjectManager = "ProjectManager";

    /// <summary>Employee: own projects and own tasks, may only change the task status.</summary>
    public const string Employee = "Employee";

    public static readonly string[] All = { Director, ProjectManager, Employee };
}
