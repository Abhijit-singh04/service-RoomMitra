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

    /// <summary>
    /// Check if AspNetUsers table exists and get its schema
    /// </summary>
    [HttpGet("aspnetusers/check")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CheckAspNetUsersTable(CancellationToken cancellationToken)
    {
        try
        {
            var connection = _db.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            // List all tables first for debugging
            using var listTablesCommand = connection.CreateCommand();
            listTablesCommand.CommandText = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                ORDER BY table_name";
            
            var allTables = new List<string>();
            using (var listReader = await listTablesCommand.ExecuteReaderAsync(cancellationToken))
            {
                while (await listReader.ReadAsync(cancellationToken))
                {
                    allTables.Add(listReader.GetString(0));
                }
            }

            // Check if table exists
            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText = @"
                SELECT EXISTS (
                    SELECT FROM information_schema.tables 
                    WHERE table_schema = 'public' 
                    AND LOWER(table_name) = LOWER('AspNetUsers')
                )";
            
            var tableExists = (bool)(await checkCommand.ExecuteScalarAsync(cancellationToken) ?? false);

            if (!tableExists)
            {
                return Ok(new 
                { 
                    exists = false,
                    allTables = allTables,
                    message = "AspNetUsers table does not exist. Use POST /api/dbtest/aspnetusers/create to create it." 
                });
            }

            // Get actual table name (to handle casing)
            using var tableNameCommand = connection.CreateCommand();
            tableNameCommand.CommandText = @"
                SELECT table_name 
                FROM information_schema.tables 
                WHERE table_schema = 'public' 
                AND LOWER(table_name) = LOWER('AspNetUsers')";
            
            var actualTableName = (string)(await tableNameCommand.ExecuteScalarAsync(cancellationToken) ?? "AspNetUsers");

            // Get table schema
            using var schemaCommand = connection.CreateCommand();
            schemaCommand.CommandText = $@"
                SELECT 
                    column_name,
                    data_type,
                    character_maximum_length,
                    is_nullable,
                    column_default
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = '{actualTableName}'
                ORDER BY ordinal_position";

            var columns = new List<object>();
            using var reader = await schemaCommand.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
            {
                columns.Add(new
                {
                    name = reader.GetString(0),
                    type = reader.GetString(1),
                    maxLength = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    nullable = reader.GetString(3),
                    defaultValue = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }

            return Ok(new 
            { 
                exists = true,
                table = actualTableName,
                columnCount = columns.Count,
                columns 
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Create AspNetUsers table using Entity Framework migrations
    /// </summary>
    [HttpPost("aspnetusers/create")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateAspNetUsersTable(CancellationToken cancellationToken)
    {
        try
        {
            // Create all ASP.NET Identity tables manually
            await _db.Database.ExecuteSqlRawAsync(@"
                -- Create AspNetUsers table
                CREATE TABLE IF NOT EXISTS ""AspNetUsers"" (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""UserName"" character varying(256),
                    ""NormalizedUserName"" character varying(256),
                    ""Email"" character varying(256),
                    ""NormalizedEmail"" character varying(256),
                    ""EmailConfirmed"" boolean NOT NULL,
                    ""PasswordHash"" text,
                    ""SecurityStamp"" text,
                    ""ConcurrencyStamp"" text,
                    ""PhoneNumber"" text,
                    ""PhoneNumberConfirmed"" boolean NOT NULL,
                    ""TwoFactorEnabled"" boolean NOT NULL,
                    ""LockoutEnd"" timestamp with time zone,
                    ""LockoutEnabled"" boolean NOT NULL,
                    ""AccessFailedCount"" integer NOT NULL,
                    ""Name"" character varying(200) NOT NULL,
                    ""ProfileImageUrl"" character varying(500),
                    ""Occupation"" character varying(100),
                    ""Bio"" character varying(1000),
                    ""Gender"" integer,
                    ""IsVerified"" boolean NOT NULL DEFAULT false,
                    ""CreatedAt"" timestamp with time zone NOT NULL,
                    ""UpdatedAt"" timestamp with time zone
                );

                -- Create indexes
                CREATE UNIQUE INDEX IF NOT EXISTS ""EmailIndex"" ON ""AspNetUsers"" (""NormalizedEmail"");
                CREATE UNIQUE INDEX IF NOT EXISTS ""UserNameIndex"" ON ""AspNetUsers"" (""NormalizedUserName"");
                CREATE INDEX IF NOT EXISTS ""IX_AspNetUsers_PhoneNumber"" ON ""AspNetUsers"" (""PhoneNumber"");
                CREATE INDEX IF NOT EXISTS ""IX_AspNetUsers_IsVerified"" ON ""AspNetUsers"" (""IsVerified"");
            ", cancellationToken);

            // Create AspNetRoles table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetRoles"" (
                    ""Id"" uuid NOT NULL PRIMARY KEY,
                    ""Name"" character varying(256),
                    ""NormalizedName"" character varying(256),
                    ""ConcurrencyStamp"" text
                );

                CREATE UNIQUE INDEX IF NOT EXISTS ""RoleNameIndex"" ON ""AspNetRoles"" (""NormalizedName"");
            ", cancellationToken);

            // Create AspNetUserRoles table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetUserRoles"" (
                    ""UserId"" uuid NOT NULL,
                    ""RoleId"" uuid NOT NULL,
                    PRIMARY KEY (""UserId"", ""RoleId""),
                    FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE,
                    FOREIGN KEY (""RoleId"") REFERENCES ""AspNetRoles"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_AspNetUserRoles_RoleId"" ON ""AspNetUserRoles"" (""RoleId"");
            ", cancellationToken);

            // Create AspNetUserClaims table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetUserClaims"" (
                    ""Id"" serial PRIMARY KEY,
                    ""UserId"" uuid NOT NULL,
                    ""ClaimType"" text,
                    ""ClaimValue"" text,
                    FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_AspNetUserClaims_UserId"" ON ""AspNetUserClaims"" (""UserId"");
            ", cancellationToken);

            // Create AspNetUserLogins table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetUserLogins"" (
                    ""LoginProvider"" character varying(128) NOT NULL,
                    ""ProviderKey"" character varying(128) NOT NULL,
                    ""ProviderDisplayName"" text,
                    ""UserId"" uuid NOT NULL,
                    PRIMARY KEY (""LoginProvider"", ""ProviderKey""),
                    FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_AspNetUserLogins_UserId"" ON ""AspNetUserLogins"" (""UserId"");
            ", cancellationToken);

            // Create AspNetUserTokens table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetUserTokens"" (
                    ""UserId"" uuid NOT NULL,
                    ""LoginProvider"" character varying(128) NOT NULL,
                    ""Name"" character varying(128) NOT NULL,
                    ""Value"" text,
                    PRIMARY KEY (""UserId"", ""LoginProvider"", ""Name""),
                    FOREIGN KEY (""UserId"") REFERENCES ""AspNetUsers"" (""Id"") ON DELETE CASCADE
                );
            ", cancellationToken);

            // Create AspNetRoleClaims table
            await _db.Database.ExecuteSqlRawAsync(@"
                CREATE TABLE IF NOT EXISTS ""AspNetRoleClaims"" (
                    ""Id"" serial PRIMARY KEY,
                    ""RoleId"" uuid NOT NULL,
                    ""ClaimType"" text,
                    ""ClaimValue"" text,
                    FOREIGN KEY (""RoleId"") REFERENCES ""AspNetRoles"" (""Id"") ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS ""IX_AspNetRoleClaims_RoleId"" ON ""AspNetRoleClaims"" (""RoleId"");
            ", cancellationToken);

            return Ok(new 
            { 
                message = "All ASP.NET Identity tables created successfully",
                tables = new[] 
                {
                    "AspNetUsers",
                    "AspNetRoles",
                    "AspNetUserRoles",
                    "AspNetUserClaims",
                    "AspNetUserLogins",
                    "AspNetUserTokens",
                    "AspNetRoleClaims"
                }
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Test reading users from AspNetUsers table
    /// </summary>
    [HttpGet("aspnetusers/read")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ReadAspNetUsers(CancellationToken cancellationToken)
    {
        try
        {
            var users = await _db.Set<RoomMitra.Infrastructure.Identity.AppUser>()
                .OrderByDescending(u => u.CreatedAt)
                .Take(50)
                .Select(u => new
                {
                    u.Id,
                    u.UserName,
                    u.Email,
                    u.Name,
                    u.PhoneNumber,
                    u.Occupation,
                    u.Bio,
                    u.Gender,
                    u.IsVerified,
                    u.CreatedAt,
                    u.UpdatedAt
                })
                .ToListAsync(cancellationToken);

            return Ok(new { count = users.Count, users });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Test creating a user in AspNetUsers table
    /// </summary>
    [HttpPost("aspnetusers/write")]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> WriteAspNetUser([FromBody] CreateTestUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = new RoomMitra.Infrastructure.Identity.AppUser
            {
                Id = Guid.NewGuid(),
                UserName = request.Email,
                NormalizedUserName = request.Email.ToUpperInvariant(),
                Email = request.Email,
                NormalizedEmail = request.Email.ToUpperInvariant(),
                EmailConfirmed = true,
                Name = request.Name,
                PhoneNumber = request.PhoneNumber,
                Occupation = request.Occupation,
                Bio = request.Bio,
                Gender = request.Gender,
                IsVerified = false,
                CreatedAt = DateTimeOffset.UtcNow,
                SecurityStamp = Guid.NewGuid().ToString(),
                LockoutEnabled = true
            };

            _db.Set<RoomMitra.Infrastructure.Identity.AppUser>().Add(user);
            await _db.SaveChangesAsync(cancellationToken);

            return StatusCode(201, new
            {
                message = "User created successfully",
                userId = user.Id,
                email = user.Email,
                name = user.Name,
                createdAt = user.CreatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Test updating a user in AspNetUsers table
    /// </summary>
    [HttpPut("aspnetusers/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateAspNetUser([FromRoute] Guid id, [FromBody] UpdateTestUserRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Set<RoomMitra.Infrastructure.Identity.AppUser>()
                .FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return NotFound(new { message = "User not found", userId = id });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
                user.Name = request.Name;
            
            if (!string.IsNullOrWhiteSpace(request.PhoneNumber))
                user.PhoneNumber = request.PhoneNumber;
            
            if (!string.IsNullOrWhiteSpace(request.Occupation))
                user.Occupation = request.Occupation;
            
            if (!string.IsNullOrWhiteSpace(request.Bio))
                user.Bio = request.Bio;
            
            if (request.Gender.HasValue)
                user.Gender = request.Gender;
            
            if (request.IsVerified.HasValue)
                user.IsVerified = request.IsVerified.Value;

            user.UpdatedAt = DateTimeOffset.UtcNow;

            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                message = "User updated successfully",
                userId = user.Id,
                name = user.Name,
                updatedAt = user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Test deleting a user from AspNetUsers table
    /// </summary>
    [HttpDelete("aspnetusers/{id:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteAspNetUser([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var user = await _db.Set<RoomMitra.Infrastructure.Identity.AppUser>()
                .FindAsync(new object[] { id }, cancellationToken);

            if (user == null)
            {
                return NotFound(new { message = "User not found", userId = id });
            }

            _db.Set<RoomMitra.Infrastructure.Identity.AppUser>().Remove(user);
            await _db.SaveChangesAsync(cancellationToken);

            return Ok(new { message = "User deleted successfully", userId = id });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    public sealed record TestWriteRequest(string Name, string? Value);
    
    public sealed record CreateTestUserRequest(
        string Email,
        string Name,
        string? PhoneNumber,
        string? Occupation,
        string? Bio,
        RoomMitra.Domain.Enums.Gender? Gender
    );
    
    public sealed record UpdateTestUserRequest(
        string? Name,
        string? PhoneNumber,
        string? Occupation,
        string? Bio,
        RoomMitra.Domain.Enums.Gender? Gender,
        bool? IsVerified
    );
}
