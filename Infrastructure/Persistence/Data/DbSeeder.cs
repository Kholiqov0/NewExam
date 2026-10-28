using Application.Identity;
using Domain.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Persistence.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        foreach (var role in Enum.GetNames<UserRole>())
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole<int>(role));
        }

        var adminEmail = "admin@mail.com";

        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new User
            {
                FirstName = "Admin",
                LastName = "Admin",
                BirthDate = new DateOnly(2000, 1, 1),
                Email = adminEmail,
                UserName = adminEmail
            };

            var result = await userManager.CreateAsync(admin, "Admin123");

            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, nameof(UserRole.Admin));
        }
    }
}
