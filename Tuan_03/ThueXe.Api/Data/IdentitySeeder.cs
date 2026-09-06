using Microsoft.EntityFrameworkCore;
using ThueXe.Api.Models;
using ThueXe.Api.Services;

namespace ThueXe.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordService passwordService)
    {
        if (await context.Users.AnyAsync()) return;

        var adminRole = new Role { Id = Guid.NewGuid(), Name = "Admin", Description = "System Administrator" };
        var staffRole = new Role { Id = Guid.NewGuid(), Name = "Staff", Description = "Rental Staff" };
        var customerRole = new Role { Id = Guid.NewGuid(), Name = "Customer", Description = "Vehicle Renter" };

        await context.Roles.AddRangeAsync(adminRole, staffRole, customerRole);

        var admin = new AppUser { Id = Guid.NewGuid(), Email = "admin@thuexe.vn", FullName = "System Admin" };
        admin.PasswordHash = passwordService.HashPassword(admin, "Admin123!");

        var staff = new AppUser { Id = Guid.NewGuid(), Email = "staff@thuexe.vn", FullName = "Rental Staff" };
        staff.PasswordHash = passwordService.HashPassword(staff, "Staff123!");

        var customer = new AppUser { Id = Guid.NewGuid(), Email = "customer@thuexe.vn", FullName = "Customer Renter" };
        customer.PasswordHash = passwordService.HashPassword(customer, "Customer123!");

        await context.Users.AddRangeAsync(admin, staff, customer);

        await context.UserRoles.AddRangeAsync(
            new UserRole { UserId = admin.Id, RoleId = adminRole.Id },
            new UserRole { UserId = staff.Id, RoleId = staffRole.Id },
            new UserRole { UserId = customer.Id, RoleId = customerRole.Id }
        );

        await context.SaveChangesAsync();
    }
}