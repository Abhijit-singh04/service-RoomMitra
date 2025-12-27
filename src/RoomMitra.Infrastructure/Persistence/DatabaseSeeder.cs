using Microsoft.EntityFrameworkCore;
using RoomMitra.Domain.Entities;
using RoomMitra.Domain.Enums;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data for development and testing
/// </summary>
public static class DatabaseSeeder
{
    public static async Task SeedAsync(RoomMitraDbContext context)
    {
        // Ensure database is created
        await context.Database.EnsureCreatedAsync();

        // Check if data already exists
        if (await context.Properties.AnyAsync() || await context.Users.AnyAsync())
        {
            return; // Database has been seeded
        }

        // Note: Amenities are seeded via AmenityConfiguration
        // Users should be created via Identity API (not direct seeding)

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Seeds sample data for development/demo purposes
    /// </summary>
    public static async Task SeedDevelopmentDataAsync(RoomMitraDbContext context)
    {
        if (await context.Properties.AnyAsync())
        {
            return; // Already has development data
        }

        // Sample properties would be added here after users are created via Identity
        // This is just a placeholder for future development data seeding

        await context.SaveChangesAsync();
    }
}
