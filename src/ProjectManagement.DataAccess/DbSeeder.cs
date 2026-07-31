using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectManagement.DataAccess.Entities;

namespace ProjectManagement.DataAccess;

/// <summary>
/// Applies pending migrations and creates the roles plus a first director account,
/// so that a freshly cloned repository can be started and signed into right away.
/// </summary>
public static class DbSeeder
{
    public const string DirectorEmail = "director@sibers.local";
    public const string DirectorPassword = "Director123!";

    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<Employee>>();
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(typeof(DbSeeder));

        await db.Database.MigrateAsync(ct);

        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        if (await userManager.FindByEmailAsync(DirectorEmail) is not null)
            return;

        var director = new Employee
        {
            UserName = DirectorEmail,
            Email = DirectorEmail,
            EmailConfirmed = true,
            FirstName = "Ivan",
            LastName = "Ivanov",
            MiddleName = "Ivanovich"
        };

        var result = await userManager.CreateAsync(director, DirectorPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(director, AppRoles.Director);
            logger.LogInformation("Seeded the initial director account {Email}.", DirectorEmail);
        }
        else
        {
            logger.LogError("Could not seed the director account: {Errors}",
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
