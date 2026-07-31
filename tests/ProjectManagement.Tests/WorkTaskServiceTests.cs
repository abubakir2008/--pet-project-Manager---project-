using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.Application.Services;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Entities.Enums;
using Xunit;

namespace ProjectManagement.Tests;

/// <summaryCovers the task logic of the first additional task
public class WorkTaskServiceTests
{
    private static WorkTaskService CreateSut(TestContext context) =>
        new(context.Tasks, context.Projects, context.Employees, new AccessControlService(context.Projects, context.Tasks));

    private static WorkTask NewTask(int projectId, string? assigneeId = null, string title = "Task", WorkTaskStatus status = WorkTaskStatus.ToDo, int priority = 1, string authorId = "manager") => new()
    {
        Title = title,
        ProjectId = projectId,
        AuthorId = authorId,
        AssigneeId = assigneeId,
        Status = status,
        Priority = priority
    };

    [Fact]
    public async Task Create_assigns_the_task_to_a_project_executor()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        var project = context.AddProject("manager", memberIds: "worker");
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new WorkTaskCreateDto
        {
            Title = "Write the report",
            ProjectId = project.Id,
            AssigneeId = "worker",
            Priority = 3
        }, TestContext.Manager("manager"));

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkTaskStatus.ToDo, result.Value!.Status);
        Assert.Equal("worker", result.Value.AssigneeId);
        Assert.Equal(project.Name, result.Value.ProjectName);
    }

    [Fact]
    public async Task Create_rejects_an_assignee_who_does_not_work_on_the_project()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("outsider");
        var project = context.AddProject("manager");
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new WorkTaskCreateDto
        {
            Title = "Task",
            ProjectId = project.Id,
            AssigneeId = "outsider"
        }, TestContext.Manager("manager"));

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_rejects_a_missing_project()
    {
        using var context = new TestContext();
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new WorkTaskCreateDto { Title = "Task", ProjectId = 404 }, TestContext.Director());

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task Create_is_refused_for_a_foreign_project_manager()
    {
        using var context = new TestContext();
        context.AddEmployee("manager1");
        context.AddEmployee("manager2");
        var project = context.AddProject("manager1");
        var sut = CreateSut(context);

        var result = await sut.CreateAsync(new WorkTaskCreateDto { Title = "Task", ProjectId = project.Id }, TestContext.Manager("manager2"));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Assignee_can_change_the_status_of_their_own_task()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        var project = context.AddProject("manager", memberIds: "worker");
        var task = NewTask(project.Id, "worker");
        context.Db.WorkTasks.Add(task);
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.ChangeStatusAsync(task.Id, new WorkTaskStatusUpdateDto { Status = WorkTaskStatus.InProgress }, TestContext.Worker("worker"));

        Assert.True(result.IsSuccess);
        Assert.Equal(WorkTaskStatus.InProgress, (await context.Tasks.GetAsync(task.Id))!.Status);
    }

    [Fact]
    public async Task Employee_cannot_change_the_status_of_a_task_of_someone_else()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        context.AddEmployee("other");
        var project = context.AddProject("manager", memberIds: new[] { "worker", "other" });
        var task = NewTask(project.Id, "worker");
        context.Db.WorkTasks.Add(task);
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.ChangeStatusAsync(task.Id, new WorkTaskStatusUpdateDto { Status = WorkTaskStatus.Done }, TestContext.Worker("other"));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }

    [Fact]
    public async Task Unknown_status_value_is_rejected()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var project = context.AddProject("manager");
        var task = NewTask(project.Id);
        context.Db.WorkTasks.Add(task);
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.ChangeStatusAsync(task.Id, new WorkTaskStatusUpdateDto { Status = (WorkTaskStatus)42 }, TestContext.Director());

        Assert.Equal(ErrorType.Validation, result.ErrorType);
    }

    [Fact]
    public async Task List_filters_by_status_and_sorts_by_title()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var project = context.AddProject("manager");
        context.Db.WorkTasks.Add(NewTask(project.Id, title: "Bravo", status: WorkTaskStatus.Done));
        context.Db.WorkTasks.Add(NewTask(project.Id, title: "Alpha", status: WorkTaskStatus.Done));
        context.Db.WorkTasks.Add(NewTask(project.Id, title: "Charlie", status: WorkTaskStatus.ToDo));
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.GetAllAsync(new WorkTaskQueryParams
        {
            Status = WorkTaskStatus.Done,
            SortBy = "title",
            Desc = false
        }, TestContext.Director());

        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(new[] { "Alpha", "Bravo" }, result.Value.Items.Select(i => i.Title));
    }

    [Fact]
    public async Task List_shows_an_employee_only_the_tasks_assigned_to_them()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        var project = context.AddProject("manager", memberIds: "worker");
        context.Db.WorkTasks.Add(NewTask(project.Id, "worker", "Mine"));
        context.Db.WorkTasks.Add(NewTask(project.Id, title: "Unassigned"));
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.GetAllAsync(new WorkTaskQueryParams(), TestContext.Worker("worker"));

        Assert.Equal("Mine", Assert.Single(result.Value!.Items).Title);
    }

    [Fact]
    public async Task Delete_is_refused_for_a_foreign_project_manager()
    {
        using var context = new TestContext();
        context.AddEmployee("manager1");
        context.AddEmployee("manager2");
        var project = context.AddProject("manager1");
        var task = NewTask(project.Id, authorId: "manager1");
        context.Db.WorkTasks.Add(task);
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        var result = await sut.DeleteAsync(task.Id, TestContext.Manager("manager2"));

        Assert.Equal(ErrorType.Forbidden, result.ErrorType);
    }
}
