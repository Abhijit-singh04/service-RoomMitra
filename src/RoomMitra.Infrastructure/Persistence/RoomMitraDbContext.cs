using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Persistence;

public sealed class RoomMitraDbContext : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>
{
    public RoomMitraDbContext(DbContextOptions<RoomMitraDbContext> options) : base(options)
    {
    }

    // Legacy - will be migrated to Property
    public DbSet<FlatListing> FlatListings => Set<FlatListing>();

    // New Schema Tables
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<PropertyAmenity> PropertyAmenities => Set<PropertyAmenity>();
    public DbSet<UserPreferences> UserPreferences => Set<UserPreferences>();
    public DbSet<OtpRequest> OtpRequests => Set<OtpRequest>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(RoomMitraDbContext).Assembly);
    }
}
