using ProjectManagement.Application.Services;
using ProjectManagement.DataAccess.Entities.Enums;
using Xunit;

namespace ProjectManagement.Tests;

/// <summary>Covers the permission matrix of the access control task.</summary>
public class AccessControlServiceTests
{
    private static AccessControlService CreateSut(TestContext context) =>
        new(context.Projects, context.Tasks);

    [Fact]
    public async Task Director_can_view_and_manage_any_project()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        var project = context.AddProject("manager");
        var sut = CreateSut(context);

        Assert.True(await sut.CanViewProjectAsync(TestContext.Director(), project.Id));
        Assert.True(await sut.CanManageProjectAsync(TestContext.Director(), project.Id));
    }

    [Fact]
    public async Task Project_manager_manages_only_own_projects()
    {
        using var context = new TestContext();
        context.AddEmployee("manager1");
        context.AddEmployee("manager2");
        var own = context.AddProject("manager1");
        var other = context.AddProject("manager2");
        var sut = CreateSut(context);

        Assert.True(await sut.CanManageProjectAsync(TestContext.Manager("manager1"), own.Id));
        Assert.False(await sut.CanManageProjectAsync(TestContext.Manager("manager1"), other.Id));
    }

    [Fact]
    public async Task Employee_views_own_project_but_cannot_manage_it()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        var project = context.AddProject("manager", memberIds: "worker");
        var sut = CreateSut(context);

        Assert.True(await sut.CanViewProjectAsync(TestContext.Worker("worker"), project.Id));
        Assert.False(await sut.CanManageProjectAsync(TestContext.Worker("worker"), project.Id));
    }

    [Fact]
    public async Task Employee_cannot_view_a_project_they_are_not_assigned_to()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("outsider");
        var project = context.AddProject("manager");
        var sut = CreateSut(context);

        Assert.False(await sut.CanViewProjectAsync(TestContext.Worker("outsider"), project.Id));
    }

    [Fact]
    public async Task Assignee_may_change_the_status_but_not_edit_the_task()
    {
        using var context = new TestContext();
        context.AddEmployee("manager");
        context.AddEmployee("worker");
        var project = context.AddProject("manager", memberIds: "worker");
        var task = new DataAccess.Entities.WorkTask
        {
            Title = "Task",
            ProjectId = project.Id,
            AuthorId = "manager",
            AssigneeId = "worker",
            Status = WorkTaskStatus.ToDo
        };
        context.Db.WorkTasks.Add(task);
        context.Db.SaveChanges();
        var sut = CreateSut(context);

        Assert.True(await sut.CanChangeTaskStatusAsync(TestContext.Worker("worker"), task.Id));
        Assert.False(await sut.CanManageTaskAsync(TestContext.Worker("worker"), task.Id));
    }

    [Fact]
    public async Task Permission_checks_return_false_for_a_missing_project()
    {
        using var context = new TestContext();
        var sut = CreateSut(context);

        Assert.False(await sut.CanViewProjectAsync(TestContext.Manager("manager"), 12345));
        Assert.False(await sut.CanManageProjectAsync(TestContext.Manager("manager"), 12345));
    }
}
