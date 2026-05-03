using LibraryManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LibraryManagementSystem.Data;

public static class DbSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var context = serviceProvider.GetRequiredService<ApplicationDbContext>();

        // Seed Roles
        string[] roleNames = { "Librarian", "Member" };
        foreach (var roleName in roleNames)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
                await roleManager.CreateAsync(new IdentityRole(roleName));
        }

        // Seed System Configuration
        if (!await context.SystemConfigurations.AnyAsync())
        {
            context.SystemConfigurations.Add(new SystemConfiguration
            {
                MaxBooksPerUser = 5,
                LoanDurationDays = 14,
                FinePerDay = 1.0m
            });
            await context.SaveChangesAsync();
        }

        // Seed Default Librarian
        await EnsureUser(userManager, "librarian@library.com", "Admin@123", "Admin Librarian", "Librarian");

        // Seed Default Member
        await EnsureUser(userManager, "member@library.com", "Member@123", "Test Member", "Member");

        // Seed KOI Librarian
        await EnsureUser(userManager, "librarian@koi.edu.au", "Admin@123", "KOI Librarian", "Librarian");

        // Jakaria Ahmed - Librarian
        await EnsureUser(userManager, "jakariaahmed023@koi.edu.au", "Web12345%$#@!", "Jakaria Ahmed", "Librarian");

        // ✅ KEY FIX: Assign "Member" role to any user who has NO role assigned
        var allUsers = await userManager.Users.ToListAsync();
        foreach (var user in allUsers)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Count == 0)
            {
                await userManager.AddToRoleAsync(user, "Member");
            }
        }
    }

    private static async Task EnsureUser(
        UserManager<ApplicationUser> userManager,
        string email, string password, string fullName, string role)
    {
        var user = await userManager.FindByEmailAsync(email);
        if (user == null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = fullName,
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(user, password);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(user, role);
        }
        else
        {
            // Make sure existing user has the right role
            if (!await userManager.IsInRoleAsync(user, role))
                await userManager.AddToRoleAsync(user, role);
        }
    }
}
