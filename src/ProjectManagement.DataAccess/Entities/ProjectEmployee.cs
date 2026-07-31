namespace ProjectManagement.DataAccess.Entities;

/// <summary>
/// Join entity for the many-to-many relation between projects and their executors:
/// one employee may work on several projects and one project may have several employees.
/// </summary>
public class ProjectEmployee
{
    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;
}
