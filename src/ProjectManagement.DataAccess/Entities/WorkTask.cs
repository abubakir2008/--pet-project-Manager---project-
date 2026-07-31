using System.ComponentModel.DataAnnotations;
using ProjectManagement.DataAccess.Entities.Enums;

namespace ProjectManagement.DataAccess.Entities;

/// <summary>
/// Project task. Named WorkTask so that it does not clash with System.Threading.Tasks.Task.
/// </summary>
public class WorkTask
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    /// <summary>Employee who created the task.</summary>
    public string AuthorId { get; set; } = string.Empty;
    public Employee Author { get; set; } = null!;

    /// <summary>A task has at most one assignee; an employee may have many tasks.</summary>
    public string? AssigneeId { get; set; }
    public Employee? Assignee { get; set; }

    public WorkTaskStatus Status { get; set; } = WorkTaskStatus.ToDo;

    public string? Comment { get; set; }

    public int Priority { get; set; }
}
