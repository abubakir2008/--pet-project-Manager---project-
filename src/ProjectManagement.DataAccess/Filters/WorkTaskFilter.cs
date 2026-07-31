using ProjectManagement.DataAccess.Entities.Enums;

namespace ProjectManagement.DataAccess.Filters;

/// <summary>Fields the task list can be sorted by.</summary>
public enum WorkTaskSortField
{
    Priority = 0,
    Title = 1,
    Status = 2
}

/// <summary>Storage-level description of a task list query.</summary>
public class WorkTaskFilter
{
    public int? ProjectId { get; set; }
    public WorkTaskStatus? Status { get; set; }
    public string? AssigneeId { get; set; }
    public string? Search { get; set; }

    /// <summary>When set, only tasks of projects managed by this employee are returned.</summary>
    public string? RestrictToProjectManagerId { get; set; }

    /// <summary>When set, only tasks assigned to this employee are returned.</summary>
    public string? RestrictToAssigneeId { get; set; }

    public WorkTaskSortField SortBy { get; set; } = WorkTaskSortField.Priority;
    public bool Descending { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
