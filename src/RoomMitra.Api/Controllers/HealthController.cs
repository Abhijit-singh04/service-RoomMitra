using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RoomMitra.Infrastructure.Options;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Api.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    private readonly RoomMitraDbContext _db;
    private readonly IOptions<AzureBlobOptions> _blobOptions;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _environment;

    public HealthController(
        RoomMitraDbContext db,
        IOptions<AzureBlobOptions> blobOptions,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        _db = db;
        _blobOptions = blobOptions;
        _configuration = configuration;
        _environment = environment;
    }

    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;

        var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";

        var dbConnection = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var dbConfigured = IsConfigured(dbConnection);

        var blobConnection = _blobOptions.Value.ConnectionString ?? string.Empty;
        var blobConfigured = IsConfigured(blobConnection);

        bool? dbCanConnect = null;
        if (dbConfigured)
        {
            dbCanConnect = await TryDbConnectAsync(cancellationToken);
        }

        return Ok(new
        {
            status = "ok",
            service = "RoomMitra.Api",
            version,
            environment = _environment.EnvironmentName,
            utcNow,
            dependencies = new
            {
                database = new
                {
                    configured = dbConfigured,
                    canConnect = dbCanConnect
                },
                blobStorage = new
                {
                    configured = blobConfigured,
                    container = _blobOptions.Value.ContainerName
                }
            }
        });
    }

    [HttpGet("live")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Live()
    {
        return Ok(new { status = "live" });
    }

    [HttpGet("ready")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        var dbConnection = _configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        var dbConfigured = IsConfigured(dbConnection);

        if (!dbConfigured)
        {
            return Ok(new { status = "ready", database = new { configured = false } });
        }

        var canConnect = await TryDbConnectAsync(cancellationToken);
        if (!canConnect)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "not_ready", database = new { configured = true, canConnect = false } });
        }

        return Ok(new { status = "ready", database = new { configured = true, canConnect = true } });
    }

    private static bool IsConfigured(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        // Heuristic: treat templates/placeholders as not configured.
        return !value.Contains("<", StringComparison.Ordinal) && !value.Contains(">", StringComparison.Ordinal);
    }

    private async Task<bool> TryDbConnectAsync(CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            return await _db.Database.CanConnectAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }
}
