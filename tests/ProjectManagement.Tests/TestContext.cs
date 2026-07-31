using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.Application.Abstractions;
using ProjectManagement.Application.Common;
using ProjectManagement.Application.Dtos;
using ProjectManagement.DataAccess;
using ProjectManagement.DataAccess.Entities;
using ProjectManagement.DataAccess.Repositories;

namespace ProjectManagement.Tests;

/// One isolated SQLite in-memory database per test, wired to the real repositories.
/// Using SQLite rather than the in-memory provider keeps relational behaviour
/// (keys, foreign keys, ordering) close to the production database.
public sealed class TestContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestContext()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_connection)
            .Options;

        Db = new ApplicationDbContext(options);
        Db.Database.EnsureCreated();

        Projects = new ProjectRepository(Db);
        Tasks = new WorkTaskRepository(Db);
        Employees = new EmployeeRepository(Db);
        Files = new FakeFileStorage();
    }

    public ApplicationDbContext Db { get; }
    public IProjectRepository Projects { get; }
    public IWorkTaskRepository Tasks { get; }
    public IEmployeeRepository Employees { get; }
    public FakeFileStorage Files { get; }

    public Employee AddEmployee(string id, string lastName = "Petrov", string firstName = "Petr")
    {
        var employee = new Employee
        {
            Id = id,
            UserName = $"{id}@test.local",
            NormalizedUserName = $"{id}@TEST.LOCAL",
            Email = $"{id}@test.local",
            NormalizedEmail = $"{id}@TEST.LOCAL",
            FirstName = firstName,
            LastName = lastName,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        Db.Users.Add(employee);
        Db.SaveChanges();
        return employee;
    }

    public Project AddProject(string managerId, string name = "Project", int priority = 1, DateTime? startDate = null, params string[] memberIds)
    {
        var project = new Project
        {
            Name = name,
            CustomerCompany = "Customer",
            ContractorCompany = "Contractor",
            StartDate = startDate ?? new DateTime(2025, 1, 1),
            EndDate = (startDate ?? new DateTime(2025, 1, 1)).AddMonths(1),
            Priority = priority,
            ManagerId = managerId
        };

        foreach (var memberId in memberIds)
            project.ProjectEmployees.Add(new ProjectEmployee { EmployeeId = memberId });

        Db.Projects.Add(project);
        Db.SaveChanges();
        return project;
    }

    public static CurrentUser Director(string id = "director") => new(id, new[] { AppRoles.Director });
    public static CurrentUser Manager(string id) => new(id, new[] { AppRoles.ProjectManager });
    public static CurrentUser Worker(string id) => new(id, new[] { AppRoles.Employee });

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}

/// File storage stub: records the calls without touching the file system.
public sealed class FakeFileStorage : IFileStorageService
{
    public List<string> Deleted { get; } = new();

    public Task<Result<(string StoredFileName, long Size)>> SaveAsync(FileUploadDto file, int projectId, CancellationToken ct = default) =>
        Task.FromResult(Result<(string, long)>.Success(($"{projectId}/{file.FileName}", file.Length)));

    public void Delete(string storedFileName) => Deleted.Add(storedFileName);

    public string GetPublicUrl(string storedFileName) => $"/uploads/{storedFileName}";
}
