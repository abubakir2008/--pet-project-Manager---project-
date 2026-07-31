using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Filters;
using ProjectManagement.DataAccess.Repositories;

namespace ProjectManagement.Application.Services;

public class ProjectService : IProjectService
{
    private const int MaxPageSize = 100;

    private readonly IProjectRepository _projects;
    private readonly IEmployeeRepository _employees;
    private readonly IAccessControlService _access;
    private readonly IFileStorageService _files;

    public ProjectService(
        IProjectRepository projects,
        IEmployeeRepository employees,
        IAccessControlService access,
        IFileStorageService files)
    {
        _projects = projects;
        _employees = employees;
        _access = access;
        _files = files;
    }

    public async Task<Result<PagedResult<ProjectListItemDto>>> GetAllAsync(ProjectQueryParams query, CurrentUser user, CancellationToken ct = default)
    {
        var filter = BuildFilter(query, user);
        var (rows, totalCount) = await _projects.GetPagedAsync(filter, ct);

        return Result<PagedResult<ProjectListItemDto>>.Success(new PagedResult<ProjectListItemDto>
        {
            Items = rows.Select(r => r.ToDto()).ToList(),
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        });
    }

    public async Task<Result<ProjectDetailsDto>> GetByIdAsync(int id, CurrentUser user, CancellationToken ct = default)
    {
        var project = await _projects.GetDetailsAsync(id, ct);
        if (project is null) return Result<ProjectDetailsDto>.NotFound("Project not found.");

        if (!await _access.CanViewProjectAsync(user, id, ct))
            return Result<ProjectDetailsDto>.Forbidden("You are not allowed to view this project.");

        return Result<ProjectDetailsDto>.Success(ToDetailsDto(project));
    }

    public async Task<Result<ProjectDetailsDto>> CreateAsync(ProjectCreateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        var managerId = string.IsNullOrWhiteSpace(dto.ManagerId) ? user.EmployeeId : dto.ManagerId;

        var validation = await ValidateAsync(dto, managerId, ct);
        if (validation.IsFailure) return Result<ProjectDetailsDto>.Failure(validation.ErrorType, validation.Errors.ToArray());

        var project = new Project
        {
            Name = dto.Name.Trim(),
            CustomerCompany = dto.CustomerCompany.Trim(),
            ContractorCompany = dto.ContractorCompany.Trim(),
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            Priority = dto.Priority,
            ManagerId = managerId
        };

        foreach (var employeeId in dto.EmployeeIds.Distinct())
            project.ProjectEmployees.Add(new ProjectEmployee { EmployeeId = employeeId });

        _projects.Add(project);
        await _projects.SaveChangesAsync(ct);

        // Reload so that the response carries the manager, employees and counters.
        var created = await _projects.GetDetailsAsync(project.Id, ct);
        return Result<ProjectDetailsDto>.Success(ToDetailsDto(created!));
    }

    public async Task<Result> UpdateAsync(int id, ProjectUpdateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        var project = await _projects.GetAsync(id, ct);
        if (project is null) return Result.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this project.");

        var managerId = string.IsNullOrWhiteSpace(dto.ManagerId) ? project.ManagerId : dto.ManagerId;

        var validation = await ValidateAsync(dto, managerId, ct);
        if (validation.IsFailure) return validation;

        project.Name = dto.Name.Trim();
        project.CustomerCompany = dto.CustomerCompany.Trim();
        project.ContractorCompany = dto.ContractorCompany.Trim();
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.Priority = dto.Priority;
        project.ManagerId = managerId;

        await _projects.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> DeleteAsync(int id, CurrentUser user, CancellationToken ct = default)
    {
        var project = await _projects.GetDetailsAsync(id, ct);
        if (project is null) return Result.NotFound("Project not found.");

        if (!user.IsDirector) return Result.Forbidden("Only a director may delete projects.");

        // Documents live outside the database, so they are removed explicitly.
        foreach (var document in project.Documents)
            _files.Delete(document.StoredFileName);

        _projects.Remove(project);
        await _projects.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> SetEmployeesAsync(int id, ProjectEmployeesUpdateDto dto, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(id, ct)) return Result.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this project.");

        var requestedIds = dto.EmployeeIds.Distinct().ToList();

        var missing = await _employees.GetMissingIdsAsync(requestedIds, ct);
        if (missing.Count > 0)
            return Result.Validation($"Unknown employees: {string.Join(", ", missing)}.");

        var currentIds = await _projects.GetMemberIdsAsync(id, ct);

        foreach (var employeeId in currentIds.Except(requestedIds))
            await _projects.RemoveMemberAsync(id, employeeId, ct);

        foreach (var employeeId in requestedIds.Except(currentIds))
            await _projects.AddMemberAsync(id, employeeId, ct);

        await _projects.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result> AddEmployeeAsync(int id, string employeeId, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(id, ct)) return Result.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this project.");

        if (await _employees.GetAsync(employeeId, ct) is null)
            return Result.Validation("Employee not found.");

        // Adding an employee twice is not an error: the end state is the requested one.
        if (!await _projects.HasMemberAsync(id, employeeId, ct))
        {
            await _projects.AddMemberAsync(id, employeeId, ct);
            await _projects.SaveChangesAsync(ct);
        }

        return Result.Success();
    }

    public async Task<Result> RemoveEmployeeAsync(int id, string employeeId, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(id, ct)) return Result.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this project.");

        if (!await _projects.RemoveMemberAsync(id, employeeId, ct))
            return Result.NotFound("This employee is not assigned to the project.");

        await _projects.SaveChangesAsync(ct);
        return Result.Success();
    }

    public async Task<Result<ProjectDocumentDto>> UploadDocumentAsync(int id, FileUploadDto file, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(id, ct)) return Result<ProjectDocumentDto>.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result<ProjectDocumentDto>.Forbidden("You are not allowed to edit this project.");

        var stored = await _files.SaveAsync(file, id, ct);
        if (stored.IsFailure) return Result<ProjectDocumentDto>.Failure(stored.ErrorType, stored.Errors.ToArray());

        var document = new ProjectDocument
        {
            ProjectId = id,
            FileName = Path.GetFileName(file.FileName),
            StoredFileName = stored.Value.StoredFileName,
            SizeBytes = stored.Value.Size
        };

        _projects.AddDocument(document);
        await _projects.SaveChangesAsync(ct);

        return Result<ProjectDocumentDto>.Success(new ProjectDocumentDto
        {
            Id = document.Id,
            FileName = document.FileName,
            SizeBytes = document.SizeBytes,
            UploadedAt = document.UploadedAt,
            Url = _files.GetPublicUrl(document.StoredFileName)
        });
    }

    public async Task<Result> DeleteDocumentAsync(int id, int documentId, CurrentUser user, CancellationToken ct = default)
    {
        if (!await _projects.ExistsAsync(id, ct)) return Result.NotFound("Project not found.");

        if (!await _access.CanManageProjectAsync(user, id, ct))
            return Result.Forbidden("You are not allowed to edit this project.");

        var document = await _projects.GetDocumentAsync(id, documentId, ct);
        if (document is null) return Result.NotFound("Document not found.");

        _files.Delete(document.StoredFileName);
        _projects.RemoveDocument(document);
        await _projects.SaveChangesAsync(ct);

        return Result.Success();
    }

    /// <summary>Translates the query string into a storage level filter and applies role visibility.</summary>
    private static ProjectFilter BuildFilter(ProjectQueryParams query, CurrentUser user)
    {
        var filter = new ProjectFilter
        {
            StartDateFrom = query.StartDateFrom,
            StartDateTo = query.StartDateTo,
            PriorityFrom = query.PriorityFrom,
            PriorityTo = query.PriorityTo,
            ManagerId = query.ManagerId,
            Search = query.Search,
            SortBy = ParseSortField(query.SortBy),
            Descending = query.Desc,
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, MaxPageSize)
        };

        if (!user.IsDirector)
        {
            if (user.IsProjectManager) filter.RestrictToManagerId = user.EmployeeId;
            else filter.RestrictToMemberId = user.EmployeeId;
        }

        return filter;
    }

    private static ProjectSortField ParseSortField(string? sortBy) => sortBy?.Trim().ToLowerInvariant() switch
    {
        "name" => ProjectSortField.Name,
        "enddate" => ProjectSortField.EndDate,
        "priority" => ProjectSortField.Priority,
        _ => ProjectSortField.StartDate
    };

    /// <summary>
    /// Checks the rules the database cannot express: date order and the existence of
    /// the referenced employees. Without this, a wrong id would surface as a foreign
    /// key violation instead of a readable message.
    /// </summary>
    private async Task<Result> ValidateAsync(ProjectCreateDto dto, string managerId, CancellationToken ct)
    {
        var errors = new List<string>();

        if (dto.EndDate.Date < dto.StartDate.Date)
            errors.Add("The end date cannot be earlier than the start date.");

        if (string.IsNullOrWhiteSpace(managerId))
            errors.Add("A project manager must be selected.");
        else if (await _employees.GetAsync(managerId, ct) is null)
            errors.Add("The selected project manager does not exist.");

        var employeeIds = dto.EmployeeIds.Distinct().ToList();
        if (employeeIds.Count > 0)
        {
            var missing = await _employees.GetMissingIdsAsync(employeeIds, ct);
            if (missing.Count > 0)
                errors.Add($"Unknown employees: {string.Join(", ", missing)}.");
        }

        return errors.Count == 0 ? Result.Success() : Result.Validation(errors.ToArray());
    }

    private ProjectDetailsDto ToDetailsDto(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        CustomerCompany = project.CustomerCompany,
        ContractorCompany = project.ContractorCompany,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        Priority = project.Priority,
        ManagerId = project.ManagerId,
        ManagerFullName = project.Manager?.FullName ?? string.Empty,
        EmployeeCount = project.ProjectEmployees.Count,
        TaskCount = project.Tasks.Count,
        Employees = project.ProjectEmployees
            .Where(pe => pe.Employee is not null)
            .Select(pe => pe.Employee.ToDto())
            .ToList(),
        Documents = project.Documents.Select(d => new ProjectDocumentDto
        {
            Id = d.Id,
            FileName = d.FileName,
            SizeBytes = d.SizeBytes,
            UploadedAt = d.UploadedAt,
            Url = _files.GetPublicUrl(d.StoredFileName)
        }).ToList()
    };
}
