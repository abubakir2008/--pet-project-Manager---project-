using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Application.Abstractions;

public interface IAccountService
{
    Task<Result<LoginResultDto>> LoginAsync(LoginDto dto, CancellationToken ct = default);
    Task<Result<LoginResultDto>> GetCurrentAsync(string employeeId, CancellationToken ct = default);
}

public interface IEmployeeService
{
    Task<Result<List<EmployeeDto>>> SearchAsync(EmployeeQueryParams query, CancellationToken ct = default);
    Task<Result<EmployeeDto>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Result<EmployeeDto>> CreateAsync(EmployeeCreateDto dto, CancellationToken ct = default);
    Task<Result> UpdateAsync(string id, EmployeeUpdateDto dto, CancellationToken ct = default);
    Task<Result> DeleteAsync(string id, CancellationToken ct = default);
}

public interface IProjectService
{
    Task<Result<PagedResult<ProjectListItemDto>>> GetAllAsync(ProjectQueryParams query, CurrentUser user, CancellationToken ct = default);
    Task<Result<ProjectDetailsDto>> GetByIdAsync(int id, CurrentUser user, CancellationToken ct = default);
    Task<Result<ProjectDetailsDto>> CreateAsync(ProjectCreateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, ProjectUpdateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CurrentUser user, CancellationToken ct = default);

    Task<Result> SetEmployeesAsync(int id, ProjectEmployeesUpdateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> AddEmployeeAsync(int id, string employeeId, CurrentUser user, CancellationToken ct = default);
    Task<Result> RemoveEmployeeAsync(int id, string employeeId, CurrentUser user, CancellationToken ct = default);

    Task<Result<ProjectDocumentDto>> UploadDocumentAsync(int id, FileUploadDto file, CurrentUser user, CancellationToken ct = default);
    Task<Result> DeleteDocumentAsync(int id, int documentId, CurrentUser user, CancellationToken ct = default);
}

public interface IWorkTaskService
{
    Task<Result<PagedResult<WorkTaskDto>>> GetAllAsync(WorkTaskQueryParams query, CurrentUser user, CancellationToken ct = default);
    Task<Result<WorkTaskDto>> GetByIdAsync(int id, CurrentUser user, CancellationToken ct = default);
    Task<Result<WorkTaskDto>> CreateAsync(WorkTaskCreateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> UpdateAsync(int id, WorkTaskUpdateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> ChangeStatusAsync(int id, WorkTaskStatusUpdateDto dto, CurrentUser user, CancellationToken ct = default);
    Task<Result> DeleteAsync(int id, CurrentUser user, CancellationToken ct = default);
}

/// <summary>Role based permission rules of the access control task.</summary>
public interface IAccessControlService
{
    Task<bool> CanViewProjectAsync(CurrentUser user, int projectId, CancellationToken ct = default);
    Task<bool> CanManageProjectAsync(CurrentUser user, int projectId, CancellationToken ct = default);
    Task<bool> CanViewTaskAsync(CurrentUser user, int taskId, CancellationToken ct = default);
    Task<bool> CanManageTaskAsync(CurrentUser user, int taskId, CancellationToken ct = default);
    Task<bool> CanChangeTaskStatusAsync(CurrentUser user, int taskId, CancellationToken ct = default);
}

public interface ITokenService
{
    (string Token, DateTime ExpiresAt) CreateToken(Employee employee, IList<string> roles);
}

/// <summary>
/// Storage of the uploaded project documents. Implemented by the presentation layer
/// (local disk); can be replaced with cloud storage without touching the logic layer.
/// </summary>
public interface IFileStorageService
{
    /// <summary>Validates and stores the file, returning its relative path and size.</summary>
    Task<Result<(string StoredFileName, long Size)>> SaveAsync(FileUploadDto file, int projectId, CancellationToken ct = default);

    void Delete(string storedFileName);

    string GetPublicUrl(string storedFileName);
}
