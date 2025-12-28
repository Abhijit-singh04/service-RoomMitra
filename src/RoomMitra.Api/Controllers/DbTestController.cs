using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Api.Controllers;

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
    /// Creates a test table in the database
    /// </summary>
    [HttpPost("setup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetupTestTable(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync(
                @"CREATE TABLE IF NOT EXISTS test_items (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name VARCHAR(255) NOT NULL,
                    value TEXT,
                    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
                )", cancellationToken);

            return Ok(new { message = "Test table created successfully", table = "test_items" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Writes a test record to the database
    /// </summary>
    [HttpPost("write")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> WriteTest([FromBody] TestWriteRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var id = Guid.NewGuid();
            await _db.Database.ExecuteSqlRawAsync(
                "INSERT INTO test_items (id, name, value) VALUES ({0}, {1}, {2})",
                id, request.Name, request.Value);

            return StatusCode(201, new 
            { 
                message = "Record created successfully", 
                id,
                name = request.Name,
                value = request.Value
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Reads all test records from the database
    /// </summary>
    [HttpGet("read")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReadTest(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, value, created_at FROM test_items ORDER BY created_at DESC LIMIT 100";

            var items = new List<object>();
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new
                {
                    id = reader.GetGuid(0),
                    name = reader.GetString(1),
                    value = reader.IsDBNull(2) ? null : reader.GetString(2),
                    createdAt = reader.GetDateTime(3)
                });
            }

            return Ok(new { count = items.Count, items });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Gets a specific test record by ID
    /// </summary>
    [HttpGet("read/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReadById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT id, name, value, created_at FROM test_items WHERE id = @id";
            var param = command.CreateParameter();
            param.ParameterName = "@id";
            param.Value = id;
            command.Parameters.Add(param);

            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                return Ok(new
                {
                    id = reader.GetGuid(0),
                    name = reader.GetString(1),
                    value = reader.IsDBNull(2) ? null : reader.GetString(2),
                    createdAt = reader.GetDateTime(3)
                });
            }

            return NotFound(new { message = "Record not found", id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a test record by ID
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteTest([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var rowsAffected = await _db.Database.ExecuteSqlRawAsync(
                "DELETE FROM test_items WHERE id = {0}", id);

            if (rowsAffected == 0)
            {
                return NotFound(new { message = "Record not found", id });
            }

            return Ok(new { message = "Record deleted successfully", id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>
    /// Cleans up the test table
    /// </summary>
    [HttpDelete("cleanup")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Cleanup(CancellationToken cancellationToken)
    {
        try
        {
            await _db.Database.ExecuteSqlRawAsync("DROP TABLE IF EXISTS test_items", cancellationToken);
            return Ok(new { message = "Test table dropped successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    public sealed record TestWriteRequest(string Name, string? Value);
}
