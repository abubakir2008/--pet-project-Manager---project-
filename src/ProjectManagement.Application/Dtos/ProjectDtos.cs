using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.Dtos;

public class ProjectListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CustomerCompany { get; set; } = string.Empty;
    public string ContractorCompany { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int Priority { get; set; }
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerFullName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public int TaskCount { get; set; }
}

public class ProjectDetailsDto : ProjectListItemDto
{
    public List<EmployeeDto> Employees { get; set; } = new();
    public List<ProjectDocumentDto> Documents { get; set; } = new();
}

public class ProjectDocumentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string Url { get; set; } = string.Empty;
}

/// <summary>
/// Payload of the create wizard: steps 1-4 are collected on the client and sent
/// in one request, because documents (step 5) need an existing project id.
/// </summary>
public class ProjectCreateDto
{
    [Required, MaxLength(200)] public string Name { get; set; } = string.Empty;
    [Required] public DateTime StartDate { get; set; }
    [Required] public DateTime EndDate { get; set; }
    [Range(1, 100)] public int Priority { get; set; } = 1;

    [Required, MaxLength(200)] public string CustomerCompany { get; set; } = string.Empty;
    [Required, MaxLength(200)] public string ContractorCompany { get; set; } = string.Empty;

    public string? ManagerId { get; set; }
    public List<string> EmployeeIds { get; set; } = new();
}

public class ProjectUpdateDto : ProjectCreateDto
{
}

public class ProjectEmployeesUpdateDto
{
    public List<string> EmployeeIds { get; set; } = new();
}

/// <summary>Filtering, sorting and paging of the project list.</summary>
public class ProjectQueryParams
{
    public DateTime? StartDateFrom { get; set; }
    public DateTime? StartDateTo { get; set; }
    public int? PriorityFrom { get; set; }
    public int? PriorityTo { get; set; }
    public string? ManagerId { get; set; }
    public string? Search { get; set; }

    /// <summary>One of: name, startDate, endDate, priority.</summary>
    public string SortBy { get; set; } = "startDate";
    public bool Desc { get; set; } = true;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>A file passed from the presentation layer without any web specific type.</summary>
public class FileUploadDto
{
    public FileUploadDto(string fileName, long length, Stream content)
    {
        FileName = fileName;
        Length = length;
        Content = content;
    }

    public string FileName { get; }
    public long Length { get; }
    public Stream Content { get; }
}
