using System.ComponentModel.DataAnnotations;
using ProjectManagement.DataAccess.Entities.Enums;

namespace ProjectManagement.Application.Dtos;

public class WorkTaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string AuthorFullName { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public string? AssigneeFullName { get; set; }
    public WorkTaskStatus Status { get; set; }
    public string? Comment { get; set; }
    public int Priority { get; set; }
}

public class WorkTaskCreateDto
{
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [Required] public int ProjectId { get; set; }
    public string? AssigneeId { get; set; }
    [MaxLength(2000)] public string? Comment { get; set; }
    [Range(1, 100)] public int Priority { get; set; } = 1;
}

public class WorkTaskUpdateDto
{
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    public string? AssigneeId { get; set; }
    public WorkTaskStatus Status { get; set; }
    [MaxLength(2000)] public string? Comment { get; set; }
    [Range(1, 100)] public int Priority { get; set; } = 1;
}

public class WorkTaskStatusUpdateDto
{
    public WorkTaskStatus Status { get; set; }
}

/// <summary>Filtering, sorting and paging of the task list.</summary>
public class WorkTaskQueryParams
{
    public int? ProjectId { get; set; }
    public WorkTaskStatus? Status { get; set; }
    public string? AssigneeId { get; set; }
    public string? Search { get; set; }

    /// <summary>One of: title, priority, status.</summary>
    public string SortBy { get; set; } = "priority";
    public bool Desc { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
