using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Database health check controller for development/debugging.
/// </summary>
[ApiController]
[Route("api/dbtest")]
public sealed class DbTestController : ControllerBase
{
    private readonly RoomMitraDbContext _db;

    public DbTestController(RoomMitraDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Simple database connection health check
    /// </summary>
    [HttpGet("health")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> HealthCheck(CancellationToken cancellationToken)
    {
        try
        {
            // Simple query to verify database connection
            var canConnect = await _db.Database.CanConnectAsync(cancellationToken);
            
            if (!canConnect)
            {
                return StatusCode(500, new { status = "unhealthy", message = "Cannot connect to database" });
            }

            // Get some basic stats
            var listingCount = await _db.FlatListings.CountAsync(cancellationToken);
            var userCount = await _db.Users.CountAsync(cancellationToken);

            return Ok(new 
            { 
                status = "healthy",
                database = "connected",
                stats = new
                {
                    listings = listingCount,
                    users = userCount
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { status = "unhealthy", error = ex.Message });
        }
    }
}
