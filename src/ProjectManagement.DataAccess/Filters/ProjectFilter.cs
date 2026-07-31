namespace ProjectManagement.DataAccess.Filters;

/// <summary>Fields the project list can be sorted by.</summary>
public enum ProjectSortField
{
    StartDate = 0,
    Name = 1,
    EndDate = 2,
    Priority = 3
}

/// <summary>
/// Storage-level description of a project list query: filtering, sorting and paging.
/// Built by the application layer from the query string, so the controllers never
/// compose LINQ expressions themselves.
/// </summary>
public class ProjectFilter
{
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public int? PriorityFrom { get; set; }
    public int? PriorityTo { get; set; }
    public string? ManagerId { get; set; }
    public string? Search { get; set; }

    /// <summary>When set, only projects managed by this employee are returned.</summary>
    public string? RestrictToManagerId { get; set; }

    /// <summary>When set, only projects this employee is assigned to are returned.</summary>
    public string? RestrictToMemberId { get; set; }

    public ProjectSortField SortBy { get; set; } = ProjectSortField.StartDate;
    public bool Descending { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
