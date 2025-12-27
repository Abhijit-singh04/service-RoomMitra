# PostgreSQL Connection Configuration

## Local Development (Docker)

### Start PostgreSQL locally with Docker:

```bash
docker run --name roommitra-postgres \
  -e POSTGRES_DB=roommitra \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=YourLocalPassword123! \
  -p 5432:5432 \
  -d postgres:16-alpine
```

### Connection String for local development:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=roommitra;Username=postgres;Password=YourLocalPassword123!;Include Error Detail=true"
  }
}
```

---

## Azure PostgreSQL Flexible Server

### 1. Create Azure PostgreSQL via Azure CLI:

```bash
# Variables
RESOURCE_GROUP="rg-roommitra"
LOCATION="eastus"
SERVER_NAME="roommitra-db"
ADMIN_USER="roommitra_admin"
ADMIN_PASSWORD="YourSecurePassword123!"
DATABASE_NAME="roommitra"

# Create resource group
az group create --name $RESOURCE_GROUP --location $LOCATION

# Create PostgreSQL Flexible Server
az postgres flexible-server create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME \
  --location $LOCATION \
  --admin-user $ADMIN_USER \
  --admin-password $ADMIN_PASSWORD \
  --sku-name Standard_B1ms \
  --tier Burstable \
  --version 16 \
  --storage-size 32 \
  --public-access 0.0.0.0

# Create database
az postgres flexible-server db create \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --database-name $DATABASE_NAME

# Configure firewall (allow Azure services)
az postgres flexible-server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME \
  --rule-name AllowAzureServices \
  --start-ip-address 0.0.0.0 \
  --end-ip-address 0.0.0.0

# Add your IP for development
az postgres flexible-server firewall-rule create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME \
  --rule-name AllowMyIP \
  --start-ip-address YOUR_IP \
  --end-ip-address YOUR_IP
```

### 2. Connection String for Azure:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=roommitra-db.postgres.database.azure.com;Database=roommitra;Username=roommitra_admin;Password=YourSecurePassword123!;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### 3. Using Azure Key Vault (Production - Recommended):

```bash
# Create Key Vault
az keyvault create \
  --name roommitra-keyvault \
  --resource-group $RESOURCE_GROUP \
  --location $LOCATION

# Store connection string
az keyvault secret set \
  --vault-name roommitra-keyvault \
  --name "ConnectionStrings--DefaultConnection" \
  --value "Host=roommitra-db.postgres.database.azure.com;Database=roommitra;Username=roommitra_admin;Password=YourSecurePassword123!;SSL Mode=Require"

# Enable managed identity for your App Service
az webapp identity assign \
  --name roommitra-api \
  --resource-group $RESOURCE_GROUP

# Grant App Service access to Key Vault
az keyvault set-policy \
  --name roommitra-keyvault \
  --object-id <app-service-managed-identity-id> \
  --secret-permissions get list
```

---

## Apply Migrations

### Local Development:
```bash
cd /Users/abhijitsingh/Projects/service-RoomMitra/src/RoomMitra.Infrastructure

# Create initial migration
dotnet ef migrations add InitialPostgreSQLSchema \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext

# Apply migration to local database
dotnet ef database update \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext
```

### Azure (Production):
```bash
# Generate SQL script (to review before applying)
dotnet ef migrations script \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext \
  --output migration.sql

# Apply to Azure (ensure connection string points to Azure)
dotnet ef database update \
  --startup-project ../RoomMitra.Api \
  --context RoomMitraDbContext
```

---

## Verify Database

### Using psql:
```bash
# Local
psql -h localhost -U postgres -d roommitra

# Azure
psql "host=roommitra-db.postgres.database.azure.com port=5432 dbname=roommitra user=roommitra_admin password=YourPassword sslmode=require"
```

### Useful SQL Commands:
```sql
-- List all tables
\dt

-- Describe a table
\d "Properties"

-- Check table counts
SELECT 
  'Users' as table_name, COUNT(*) as count FROM "AspNetUsers"
UNION ALL
SELECT 'Properties', COUNT(*) FROM "Properties"
UNION ALL
SELECT 'PropertyImages', COUNT(*) FROM "PropertyImages"
UNION ALL
SELECT 'Amenities', COUNT(*) FROM "Amenities";

-- Check if amenities were seeded
SELECT * FROM "Amenities";
```

---

## NuGet Packages Required

Ensure these packages are installed in `RoomMitra.Infrastructure`:

```bash
cd src/RoomMitra.Infrastructure

dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
```

---

## Update Program.cs

```csharp
// In RoomMitra.Api/Program.cs

builder.Services.AddDbContext<RoomMitraDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null));
});
```

---

## Backup Strategy

### Automated Backups (Azure):
```bash
# Enable automated backups (enabled by default)
az postgres flexible-server parameter set \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --name backup_retention_days \
  --value 7

# Create manual backup (Point-in-time restore)
az postgres flexible-server backup create \
  --resource-group $RESOURCE_GROUP \
  --name $SERVER_NAME
```

---

## Monitoring

### Enable Query Performance Insights:
```bash
az postgres flexible-server parameter set \
  --resource-group $RESOURCE_GROUP \
  --server-name $SERVER_NAME \
  --name pg_stat_statements.track \
  --value all
```

### View slow queries:
```sql
SELECT query, calls, total_time, mean_time
FROM pg_stat_statements
ORDER BY mean_time DESC
LIMIT 10;
```

---

## Cost Estimation (Azure)

| Resource | Tier | Monthly Cost (USD) |
|----------|------|-------------------|
| PostgreSQL Flexible Server | Basic (B1ms) | ~$12-15 |
| Blob Storage (100GB) | Hot tier | ~$2-3 |
| **Total (MVP)** | | **~$15-20** |

---

## Next Steps Checklist

- [ ] Choose deployment option (local Docker or Azure)
- [ ] Configure connection string in appsettings
- [ ] Install required NuGet packages
- [ ] Create EF Core migration
- [ ] Apply migration to database
- [ ] Verify tables and seeded amenities
- [ ] Test CRUD operations
- [ ] Set up Azure Blob Storage for images
- [ ] Implement property creation API
- [ ] Add search/filter endpoints

---

## Troubleshooting

### Migration Fails:
```bash
# Remove last migration
dotnet ef migrations remove --startup-project ../RoomMitra.Api

# Clean and rebuild
dotnet clean
dotnet build
```

### Connection Issues (Azure):
- Check firewall rules in Azure Portal
- Verify SSL mode is set to "Require"
- Ensure password doesn't have special characters that need escaping
- Test connection using Azure Data Studio or pgAdmin

### Performance Issues:
- Check query execution plans
- Verify indexes are created
- Enable connection pooling
- Consider adding Redis cache for hot data
