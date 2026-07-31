using System.ComponentModel.DataAnnotations;

namespace ProjectManagement.Application.Dtos;

public class EmployeeDto
{
    public string Id { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName => $"{LastName} {FirstName} {MiddleName}".Trim();
    public IList<string> Roles { get; set; } = new List<string>();
}

public class EmployeeCreateDto
{
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(100)] public string? MiddleName { get; set; }
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(6), MaxLength(100)] public string Password { get; set; } = string.Empty;
    [Required] public string Role { get; set; } = "Employee";
}

public class EmployeeUpdateDto
{
    [Required, MaxLength(100)] public string FirstName { get; set; } = string.Empty;
    [Required, MaxLength(100)] public string LastName { get; set; } = string.Empty;
    [MaxLength(100)] public string? MiddleName { get; set; }
    [Required, EmailAddress, MaxLength(256)] public string Email { get; set; } = string.Empty;
    public string? Role { get; set; }
}

/// <summary>Query string of the employee autocomplete.</summary>
public class EmployeeQueryParams
{
    public string? Search { get; set; }

    /// <summary>Requested page size; clamped by the logic layer to a safe maximum.</summary>
    public int Take { get; set; } = 20;
}
