using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.DataAccess;

public class ApplicationDbContext : IdentityDbContext<Employee, IdentityRole, string>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectEmployee> ProjectEmployees => Set<ProjectEmployee>();
    public DbSet<WorkTask> WorkTasks => Set<WorkTask>();
    public DbSet<ProjectDocument> ProjectDocuments => Set<ProjectDocument>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Composite key for the many-to-many relation Project <-> Employee.
        builder.Entity<ProjectEmployee>().HasKey(pe => new { pe.ProjectId, pe.EmployeeId });

        builder.Entity<ProjectEmployee>()
            .HasOne(pe => pe.Project)
            .WithMany(p => p.ProjectEmployees)
            .HasForeignKey(pe => pe.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ProjectEmployee>()
            .HasOne(pe => pe.Employee)
            .WithMany(e => e.ProjectEmployees)
            .HasForeignKey(pe => pe.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // Deleting an employee must not silently delete the projects they manage:
        // the application layer reports a conflict instead.
        builder.Entity<Project>()
            .HasOne(p => p.Manager)
            .WithMany(e => e.ManagedProjects)
            .HasForeignKey(p => p.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkTask>()
            .HasOne(t => t.Project)
            .WithMany(p => p.Tasks)
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WorkTask>()
            .HasOne(t => t.Author)
            .WithMany(e => e.AuthoredTasks)
            .HasForeignKey(t => t.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<WorkTask>()
            .HasOne(t => t.Assignee)
            .WithMany(e => e.AssignedTasks)
            .HasForeignKey(t => t.AssigneeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ProjectDocument>()
            .HasOne(d => d.Project)
            .WithMany(p => p.Documents)
            .HasForeignKey(d => d.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for the fields the project/task lists are filtered and sorted by.
        builder.Entity<Project>().HasIndex(p => p.Priority);
        builder.Entity<Project>().HasIndex(p => p.StartDate);
        builder.Entity<WorkTask>().HasIndex(t => t.Status);
    }
}
