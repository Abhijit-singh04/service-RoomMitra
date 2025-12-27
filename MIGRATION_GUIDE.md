# Migration Guide: SQL Server to PostgreSQL

## Why Switch to PostgreSQL?

Your current setup uses SQL Server, but PostgreSQL is better for RoomMitra because:

✅ **Cost-effective** - Free and open-source  
✅ **Better for Azure** - Azure PostgreSQL Flexible Server is optimized and cheaper  
✅ **Better JSON support** - Native JSONB for flexible data  
✅ **Better geospatial** - PostGIS extension for location features  
✅ **Cross-platform** - Works on Windows, Mac, Linux  
✅ **Industry standard** - Used by Uber, Instagram, Netflix  

---

## Step-by-Step Migration

### Step 1: Update NuGet Packages

```bash
cd src/RoomMitra.Infrastructure

# Remove SQL Server package
dotnet remove package Microsoft.EntityFrameworkCore.SqlServer

# Add PostgreSQL package
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.11
```

**Your updated RoomMitra.Infrastructure.csproj should have:**
```xml
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.11" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.18" />
<PackageReference Include="Microsoft.AspNetCore.Identity.EntityFrameworkCore" Version="8.0.18" />
<PackageReference Include="Azure.Storage.Blobs" Version="12.26.0" />
```

---

### Step 2: Update DependencyInjection.cs

Find where you register DbContext (likely in `RoomMitra.Infrastructure/DependencyInjection.cs` or `Program.cs`)

**BEFORE (SQL Server):**
```csharp
services.AddDbContext<RoomMitraDbContext>(options =>
    options.UseSqlServer(
        configuration.GetConnectionString("DefaultConnection")));
```

**AFTER (PostgreSQL):**
```csharp
services.AddDbContext<RoomMitraDbContext>(options =>
    options.UseNpgsql(
        configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null)));
```

---

### Step 3: Update Connection String

**appsettings.Development.json:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=roommitra;Username=postgres;Password=YourPassword123!;Include Error Detail=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

**appsettings.json (for Azure):**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=roommitra-db.postgres.database.azure.com;Database=roommitra;Username=roommitra_admin;Password=PLACEHOLDER;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

### Step 4: Start PostgreSQL Locally

**Option A: Docker (Recommended)**
```bash
docker run --name roommitra-postgres \
  -e POSTGRES_DB=roommitra \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourPassword123! \
  -p 5432:5432 \
  -d postgres:16-alpine
```

**Option B: Install Postgres.app (Mac)**
- Download from https://postgresapp.com/
- Start the app
- Database will be available at localhost:5432

**Option C: Install PostgreSQL via Homebrew**
```bash
brew install postgresql@16
brew services start postgresql@16
createdb roommitra
```

---

### Step 5: Remove Old Migrations

Since you're switching databases, remove existing SQL Server migrations:

```bash
cd src/RoomMitra.Infrastructure

# Remove Migrations folder entirely
rm -rf Migrations/
```

---

### Step 6: Create New PostgreSQL Migrations

```bash
cd src/RoomMitra.Infrastructure

# Create initial migration for PostgreSQL
dotnet ef migrations add InitialPostgreSQLSchema \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext \
  --output-dir Migrations

# Apply migration to create tables
dotnet ef database update \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext
```

---

### Step 7: Verify Database

**Using psql:**
```bash
# Connect to database
psql -h localhost -U postgres -d roommitra

# List all tables
\dt

# Check Users table
\d "AspNetUsers"

# Check Properties table
\d "Properties"

# Verify amenities were seeded
SELECT * FROM "Amenities";

# Exit
\q
```

**Using DBeaver/pgAdmin:**
- Connect to `localhost:5432`
- Database: `roommitra`
- Username: `postgres`
- Password: `YourPassword123!`

---

### Step 8: Test Your Application

```bash
cd src/RoomMitra.Api

# Run the application
dotnet run

# Or with watch
dotnet watch run
```

Test the endpoints:
- POST /api/auth/register - Create a user
- POST /api/auth/login - Login
- GET /api/health - Health check

---

## Common Issues & Solutions

### Issue 1: "Npgsql.PostgresException: 42P01: relation does not exist"

**Solution:** You haven't run migrations yet
```bash
dotnet ef database update --startup-project ../RoomMitra.Api
```

---

### Issue 2: "Password authentication failed"

**Solution:** Check your connection string password matches Docker container
```bash
# Check running containers
docker ps

# View container logs
docker logs roommitra-postgres

# Restart container
docker restart roommitra-postgres
```

---

### Issue 3: "Port 5432 is already in use"

**Solution:** Another PostgreSQL instance is running
```bash
# Check what's using port 5432
lsof -i :5432

# Stop other PostgreSQL services
brew services stop postgresql@16

# Or use a different port in Docker
docker run --name roommitra-postgres \
  -e POSTGRES_DB=roommitra \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourPassword123! \
  -p 5433:5432 \
  -d postgres:16-alpine

# Update connection string to use port 5433
```

---

### Issue 4: Migration doesn't recognize new entities

**Solution:** Clean and rebuild
```bash
# Clean solution
dotnet clean

# Rebuild
dotnet build

# Remove migration
dotnet ef migrations remove --startup-project ../RoomMitra.Api

# Recreate
dotnet ef migrations add InitialPostgreSQLSchema --startup-project ../RoomMitra.Api
```

---

## Data Migration (If you have existing data)

If you have existing data in SQL Server that needs to be migrated:

### 1. Export data from SQL Server
```bash
# Use SQL Server Management Studio to export as CSV
# Or use bcp utility
```

### 2. Import to PostgreSQL
```sql
-- Example for Users table
COPY "AspNetUsers" FROM '/path/to/users.csv' DELIMITER ',' CSV HEADER;
```

### 3. Or use a migration tool
- **pgLoader** - Automatic migration from SQL Server to PostgreSQL
- **AWS Database Migration Service**
- **Azure Database Migration Service**

---

## Differences Between SQL Server and PostgreSQL

| Feature | SQL Server | PostgreSQL |
|---------|-----------|------------|
| String quotes | Single `'` or double `"` | Only single `'` |
| Case sensitivity | No | Yes (use `"Quotes"` for exact case) |
| GUID | UNIQUEIDENTIFIER | UUID |
| Auto-increment | IDENTITY | SERIAL or gen_random_uuid() |
| JSON | nvarchar(max) | JSONB (native) |
| Arrays | Not native | Native support |
| TOP N | `SELECT TOP 10` | `LIMIT 10` |

---

## PostgreSQL-Specific Features You Can Now Use

### 1. Array Columns
```csharp
public List<string> Tags { get; set; } = new();

// EF Core will map this to TEXT[] in PostgreSQL
```

### 2. JSONB for flexible data
```csharp
public JsonDocument Metadata { get; set; }

// Stored as JSONB - queryable and indexed
```

### 3. Full-Text Search
```sql
CREATE INDEX idx_property_search 
ON "Properties" 
USING gin(to_tsvector('english', "Title" || ' ' || "Description"));
```

### 4. PostGIS for Geospatial (future enhancement)
```bash
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL.NetTopologySuite

# Enable in DbContext
options.UseNpgsql(connectionString, 
    o => o.UseNetTopologySuite());
```

---

## Rollback Plan

If something goes wrong and you need to go back to SQL Server:

```bash
# 1. Reinstall SQL Server package
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# 2. Remove PostgreSQL package
dotnet remove package Npgsql.EntityFrameworkCore.PostgreSQL

# 3. Change UseSqlServer in DependencyInjection
# 4. Update connection string
# 5. Run migrations
```

---

## Next Steps After Migration

✅ Database migrated to PostgreSQL  
⏭️ Set up Azure PostgreSQL Flexible Server  
⏭️ Configure Azure Blob Storage for images  
⏭️ Implement property creation endpoint  
⏭️ Add search/filter APIs  
⏭️ Set up CI/CD for migrations  
⏭️ Configure automated backups  

---

## Quick Commands Reference

```bash
# Start PostgreSQL (Docker)
docker start roommitra-postgres

# Stop PostgreSQL
docker stop roommitra-postgres

# View PostgreSQL logs
docker logs -f roommitra-postgres

# Access PostgreSQL CLI
docker exec -it roommitra-postgres psql -U postgres -d roommitra

# Create migration
dotnet ef migrations add MigrationName --startup-project ../RoomMitra.Api

# Apply migrations
dotnet ef database update --startup-project ../RoomMitra.Api

# Generate SQL script
dotnet ef migrations script --startup-project ../RoomMitra.Api

# Remove last migration
dotnet ef migrations remove --startup-project ../RoomMitra.Api

# Drop database (CAUTION!)
dotnet ef database drop --startup-project ../RoomMitra.Api
```

---

## Support & Resources

- **PostgreSQL Docs**: https://www.postgresql.org/docs/
- **Npgsql EF Core**: https://www.npgsql.org/efcore/
- **Azure PostgreSQL**: https://learn.microsoft.com/en-us/azure/postgresql/
- **pgAdmin**: https://www.pgadmin.org/ (GUI tool)
- **DBeaver**: https://dbeaver.io/ (Multi-DB GUI tool)

You're all set! Your PostgreSQL schema is production-ready. 🚀
