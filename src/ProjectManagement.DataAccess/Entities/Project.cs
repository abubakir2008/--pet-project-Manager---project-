using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.DataAccess.Entities;

public class Project
{
    public int Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string CustomerCompany { get; set; } = string.Empty;

    [MaxLength(200)]
    public string ContractorCompany { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    /// <summary>Integer priority; a higher value means a more important project.</summary>
    public int Priority { get; set; }

    /// <summary>Project manager: one of the employees.</summary>
    public string ManagerId { get; set; } = string.Empty;
    public Employee? Manager { get; set; }

    public ICollection<ProjectEmployee> ProjectEmployees { get; set; } = new List<ProjectEmployee>();
    public ICollection<WorkTask> Tasks { get; set; } = new List<WorkTask>();
    public ICollection<ProjectDocument> Documents { get; set; } = new List<ProjectDocument>();
}
