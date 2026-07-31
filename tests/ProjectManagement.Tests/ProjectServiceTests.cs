using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.Application.Services;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>Covers validation, role visibility, filtering and sorting of the project logic.</summary>
public class ProjectServiceTests
{
    private static ProjectService CreateSut(TestContext context) =>
        new(context.Projects, context.Employees, new AccessControlService(context.Projects, context.Tasks), context.Files);

    private static ProjectCreateDto ValidDto(string managerId) => new()
    {
        Name = "New project",
        CustomerCompany = "Customer",
        ContractorCompany = "Contractor",
        StartDate = new DateTime(2025, 3, 1),
        EndDate = new DateTime(2025, 6, 1),
        Priority = 5,
        ManagerId = managerId
    };

    [Fact]
    public async Task Create_stores_the_project_with_its_executors()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker1");
        context.AddEmployee("worker2");
        var sut = CreateSut(context);

        var dto = ValidDto("manager");
        dto.EmployeeIds = new List<string> { "worker1", "worker2", "worker1" };

        var result = await sut.CreateAsync(dto, TestContext.Director());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.EmployeeCount);
        Assert.Equal(2, result.Value.Employees.Count);
        Assert.Equal("New project", result.Value.Name);
    }

    [Fact]
    public async Task Create_rejects_an_end_date_before_the_start_date()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var sut = CreateSut(context);

        var dto = ValidDto("manager");
        dto.EndDate = dto.StartDate.AddDays(-1);

        var result = await sut.CreateAsync(dto, TestContext.Director());

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_rejects_an_unknown_manager_instead_of_failing_on_the_foreign_key()
    {
        using var context = new TestContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(ValidDto("ghost"), TestContext.Director());

        Assert.Equal(ErrorType.Validation, result.ErrorType);
        Assert.Contains("project manager", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Create_rejects_unknown_executors()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var sut = CreateSut(context);

        var dto = ValidDto("manager");
        dto.EmployeeIds = new List<string> { "ghost" };

        var result = await sut.CreateAsync(dto, TestContext.Director());

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Details_report_the_number_of_tasks_of_the_project()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var project = context.AddProject("manager");
        context.Db.WorkTasks.Add(new DataAccess.Entities.WorkTask { Title = "T1", ProjectId = project.Id, AuthorId = "manager" });
        context.Db.WorkTasks.Add(new DataAccess.Entities.WorkTask { Title = "T2", ProjectId = project.Id, AuthorId = "manager" });
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(project.Id, TestContext.Director());

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.TaskCount);
    }

    [Fact]
    public async Task List_shows_a_project_manager_only_their_own_projects()
    {
        using var context = new TestContext();
        context.AddEmployee("manager1");
        context.AddEmployee("manager2");
        context.AddProject("manager1", "Mine");
        context.AddProject("manager2", "Theirs");
        var sut = CreateSut(context);

        var result = await sut.GetAllAsync(new ProjectQueryParams(), TestContext.Manager("manager1"));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Equal("Mine", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task List_shows_an_employee_only_the_projects_they_are_assigned_to()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        context.AddProject("manager", "Assigned", memberIds: "worker");
        context.AddProject("manager", "Not assigned");
        var sut = CreateSut(context);

        var result = await sut.GetAllAsync(new ProjectQueryParams(), TestContext.Worker("worker"));

        Assert.Equal(1, result.Value!.TotalCount);
        Assert.Equal("Assigned", result.Value.Items[0].Name);
    }

    [Fact]
    public async Task List_filters_by_start_date_range_and_priority()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddProject("manager", "January", priority: 1, startDate: new DateTime(2025, 1, 10));
        context.AddProject("manager", "June", priority: 9, startDate: new DateTime(2025, 6, 10));
        var sut = CreateSut(context);

        var byDate = await sut.GetAllAsync(new ProjectQueryParams
        {
            StartDateFrom = new DateTime(2025, 5, 1),
            StartDateTo = new DateTime(2025, 7, 1)
        }, TestContext.Director());

        var byPriority = await sut.GetAllAsync(new ProjectQueryParams { PriorityFrom = 5 }, TestContext.Director());

        Assert.Equal("June", Assert.Single(byDate.Value!.Items).Name);
        Assert.Equal("June", Assert.Single(byPriority.Value!.Items).Name);
    }

    [Fact]
    public async Task List_sorts_by_the_requested_field()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddProject("manager", "Bravo", priority: 1);
        context.AddProject("manager", "Alpha", priority: 9);
        var sut = CreateSut(context);

        var byName = await sut.GetAllAsync(new ProjectQueryParams { SortBy = "name", Desc = false }, TestContext.Director());
        var byPriorityDesc = await sut.GetAllAsync(new ProjectQueryParams { SortBy = "priority", Desc = true }, TestContext.Director());

        Assert.Equal(new[] { "Alpha", "Bravo" }, byName.Value!.Items.Select(i => i.Name));
        Assert.Equal(new[] { "Alpha", "Bravo" }, byPriorityDesc.Value!.Items.Select(i => i.Name));
    }

    [Fact]
    public async Task List_clamps_an_oversized_page_size()
    {
        using var context = new TestContext();
        var sut = CreateSut(context);

        var result = await sut.GetAllAsync(new ProjectQueryParams { PageSize = 100_000, Page = 0 }, TestContext.Director());

        Assert.Equal(100, result.Value!.PageSize);
        Assert.Equal(1, result.Value.Page);
    }

    [Fact]
    public async Task SetEmployees_adds_and_removes_so_that_the_set_matches_the_request()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("keep");
        context.AddEmployee("remove");
        context.AddEmployee("add");
        var project = context.AddProject("manager", memberIds: new[] { "keep", "remove" });
        var sut = CreateSut(context);

        var result = await sut.SetEmployeesAsync(
            project.Id,
            new ProjectEmployeesUpdateDto { EmployeeIds = new List<string> { "keep", "add" } },
            TestContext.Director());

        Assert.True(result.IsSuccess);
        var members = await context.Projects.GetMemberIdsAsync(project.Id);
        Assert.Equal(new[] { "add", "keep" }, members.OrderBy(m => m));
    }

    [Fact]
    public async Task SetEmployees_is_refused_for_a_foreign_project_manager()
    {
        using var context = new TestContext();
        context.AddEmployee("manager1");
        context.AddEmployee("manager2");
        var project = context.AddProject("manager1");
        var sut = CreateSut(context);

        var result = await sut.SetEmployeesAsync(
            project.Id,
            new ProjectEmployeesUpdateDto(),
            TestContext.Manager("manager2"));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Delete_is_refused_for_a_project_manager_and_removes_the_documents_for_a_director()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var project = context.AddProject("manager");
        context.Db.ProjectDocuments.Add(new DataAccess.Entities.ProjectDocument
        {
            ProjectId = project.Id,
            FileName = "spec.pdf",
            StoredFileName = $"{project.Id}/spec.pdf",
            SizeBytes = 10
        });
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var refused = await sut.DeleteAsync(project.Id, TestContext.Manager("manager"));
        Assert.Equal(ErrorType.Forbidden, refused.ErrorType);

        var deleted = await sut.DeleteAsync(project.Id, TestContext.Director());
        Assert.True(deleted.IsSuccess);
        Assert.Contains($"{project.Id}/spec.pdf", context.Files.Deleted);
    }

    [Fact]
    public async Task Missing_project_is_reported_as_not_found()
    {
        using var context = new TestContext();
        var sut = CreateSut(context);

        var result = await sut.GetByIdAsync(999, TestContext.Director());

        Assert.Equal(ErrorType.NotFound, result.ErrorType);
    }
}
