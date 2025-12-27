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

    public DbSet<FlatListing> FlatListings => Set<FlatListing>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(RoomMitraDbContext).Assembly);
    }
}
