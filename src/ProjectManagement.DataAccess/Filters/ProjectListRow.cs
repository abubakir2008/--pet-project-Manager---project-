namespace ProjectManagement.DataAccess.Filters;

/// <summary>
/// Flat row of the project list. Counts are computed by the database, so the list query
/// never loads the employee and task collections of every project.
/// </summary>
public class ProjectListRow
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CustomerCompany { get; set; } = string.Empty;
    public string ContractorCompany { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }
    public string ManagerId { get; set; } = string.Empty;
    public string? ManagerFirstName { get; set; }
    public string? ManagerLastName { get; set; }
    public string? ManagerMiddleName { get; set; }
    public int EmployeeCount { get; set; }
    public int TaskCount { get; set; }
}
