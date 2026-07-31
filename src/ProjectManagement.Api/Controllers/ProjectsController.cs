using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProjectManagement.Api.Infrastructure;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.Api.Controllers;

[Route("api/projects")]
[Authorize]
public class ProjectsController : ApiControllerBase
{
    private readonly IProjectService _projects;

    public ProjectsController(IProjectService projects) => _projects = projects;

    /// <summary>
    /// Project list with filtering (start date range, priority range, manager, text search),
    /// sorting by the main fields and paging.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProjectListItemDto>>> GetAll([FromQuery] ProjectQueryParams query, CancellationToken ct) =>
        FromResult(await _projects.GetAllAsync(query, CurrentUser, ct));

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProjectDetailsDto>> GetById(int id, CancellationToken ct) =>
        FromResult(await _projects.GetByIdAsync(id, CurrentUser, ct));

    /// <summary>Creates a project from the data collected on wizard steps 1-4.</summary>
    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<ActionResult<ProjectDetailsDto>> Create(ProjectCreateDto dto, CancellationToken ct)
    {
        var result = await _projects.CreateAsync(dto, CurrentUser, ct);
        return FromCreatedResult(result, nameof(GetById), new { id = result.Value?.Id });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> Update(int id, ProjectUpdateDto dto, CancellationToken ct) =>
        FromResult(await _projects.UpdateAsync(id, dto, CurrentUser, ct));

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Director)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct) =>
        FromResult(await _projects.DeleteAsync(id, CurrentUser, ct));

    /// <summary>Replaces the whole set of project executors (wizard step 4).</summary>
    [HttpPut("{id:int}/employees")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> SetEmployees(int id, ProjectEmployeesUpdateDto dto, CancellationToken ct) =>
        FromResult(await _projects.SetEmployeesAsync(id, dto, CurrentUser, ct));

    [HttpPost("{id:int}/employees/{employeeId}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> AddEmployee(int id, string employeeId, CancellationToken ct) =>
        FromResult(await _projects.AddEmployeeAsync(id, employeeId, CurrentUser, ct));

    [HttpDelete("{id:int}/employees/{employeeId}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> RemoveEmployee(int id, string employeeId, CancellationToken ct) =>
        FromResult(await _projects.RemoveEmployeeAsync(id, employeeId, CurrentUser, ct));

    /// <summary>Wizard step 5: upload of a project document (drag and drop on the client).</summary>
    [HttpPost("{id:int}/documents")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    [RequestSizeLimit(FileStorageService.MaxSizeBytes + 1024 * 1024)]
    public async Task<ActionResult<ProjectDocumentDto>> UploadDocument(int id, IFormFile? file, CancellationToken ct)
    {
        if (file is null)
            return BadRequest(new ProblemDetails { Status = StatusCodes.Status400BadRequest, Title = "Invalid request", Detail = "No file was sent." });

        await using var content = file.OpenReadStream();
        var upload = new FileUploadDto(file.FileName, file.Length, content);

        return FromResult(await _projects.UploadDocumentAsync(id, upload, CurrentUser, ct));
    }

    [HttpDelete("{id:int}/documents/{documentId:int}")]
    [Authorize(Roles = $"{AppRoles.Director},{AppRoles.ProjectManager}")]
    public async Task<IActionResult> DeleteDocument(int id, int documentId, CancellationToken ct) =>
        FromResult(await _projects.DeleteDocumentAsync(id, documentId, CurrentUser, ct));
}
