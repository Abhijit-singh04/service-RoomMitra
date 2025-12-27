# RoomMitra Database Schema - Azure PostgreSQL

## Overview
This document describes the complete database schema for RoomMitra, designed for **Azure PostgreSQL Flexible Server**.

## Why PostgreSQL?

### ✅ Perfect for RoomMitra because:
- **Clear relationships**: User → Properties (one-to-many)
- **Easy filtering**: City, rent range, room type, gender preferences
- **Strong consistency**: ACID transactions for bookings/payments
- **Easy migrations**: EF Core migrations support
- **Scalability**: Azure Flexible Server can scale as you grow
- **Native .NET support**: Perfect integration with Entity Framework Core

## Architecture

```
Client (Web/Mobile App)
       |
   API (.NET)
       |
PostgreSQL  <------>  Azure Blob Storage
       |                    (Images/Videos)
  (Optional)
   Redis Cache
```

## Core Tables

### 1. Users (AspNetUsers)
Extended Identity table with custom fields.

```sql
Users
-----
Id                  UUID (PK)
Name                VARCHAR(200)
Email               VARCHAR(256) UNIQUE
PhoneNumber         VARCHAR(50)
Gender              INT (Enum: Male=1, Female=2, Any=3)
IsVerified          BOOLEAN DEFAULT FALSE
ProfileImageUrl     VARCHAR(500)
Occupation          VARCHAR(100)
Bio                 VARCHAR(1000)
CreatedAt           TIMESTAMPTZ
UpdatedAt           TIMESTAMPTZ
```

**Indexes:**
- Unique on Email
- Index on PhoneNumber
- Index on IsVerified

---

### 2. Properties
Core table for flat listings.

```sql
Properties
----------
Id                  UUID (PK)
UserId              UUID (FK → Users.Id)

Title               VARCHAR(200)
Description         VARCHAR(4000)
PropertyType        INT (1BHK=1, 2BHK=2, 3BHK=3, PG=4, Shared=5)

Rent                DECIMAL(18,2)
Deposit             DECIMAL(18,2)
AvailableFrom       DATE

City                VARCHAR(100)
Area                VARCHAR(200)
Address             VARCHAR(500)
Latitude            DECIMAL(10,7)
Longitude           DECIMAL(10,7)

PreferredGender     INT (Male=1, Female=2, Any=3)
PreferredFood       INT (Veg=1, NonVeg=2, Any=3)
Furnishing          INT (Unfurnished=1, SemiFurnished=2, Furnished=3)

Status              INT (Active=1, Inactive=2, Rented=3)
CreatedAt           TIMESTAMPTZ
UpdatedAt           TIMESTAMPTZ
```

**Indexes:**
- Index on UserId
- Index on City
- Index on Rent
- Index on PropertyType
- Index on Status
- Index on AvailableFrom
- Composite index on (City, Rent, Status) for search optimization

---

### 3. PropertyImages
Stores URLs to images in Azure Blob Storage.

```sql
PropertyImages
--------------
Id                  UUID (PK)
PropertyId          UUID (FK → Properties.Id)

ImageUrl            VARCHAR(1000)  -- Azure Blob URL
IsCover             BOOLEAN DEFAULT FALSE
DisplayOrder        INT DEFAULT 0

CreatedAt           TIMESTAMPTZ
UpdatedAt           TIMESTAMPTZ
```

**Blob Storage Pattern:**
```
/properties/{propertyId}/images/{imageId}.jpg
```

**Indexes:**
- Index on PropertyId
- Composite index on (PropertyId, IsCover)
- Composite index on (PropertyId, DisplayOrder)

---

### 4. Amenities
Master table for all amenities.

```sql
Amenities
---------
Id                  UUID (PK)
Name                VARCHAR(100) UNIQUE
Description         VARCHAR(500)
Icon                VARCHAR(100)  -- For UI icons
```

**Pre-seeded amenities:**
- WiFi
- Air Conditioning
- Washing Machine
- Parking
- Gym
- Power Backup
- Water Purifier
- Refrigerator
- TV
- Security (24/7)

---

### 5. PropertyAmenities
Many-to-many join table between Properties and Amenities.

```sql
PropertyAmenities
-----------------
PropertyId          UUID (FK → Properties.Id)
AmenityId           UUID (FK → Amenities.Id)
CreatedAt           TIMESTAMPTZ

PRIMARY KEY (PropertyId, AmenityId)
```

**Indexes:**
- Index on PropertyId
- Index on AmenityId

---

### 6. UserPreferences
User's search preferences for matching.

```sql
UserPreferences
---------------
Id                      UUID (PK)
UserId                  UUID (FK → Users.Id) UNIQUE

BudgetMin               DECIMAL(18,2)
BudgetMax               DECIMAL(18,2)
PreferredCity           VARCHAR(100)
PreferredAreas          JSONB  -- Array of area names
PreferredGender         INT (nullable)
PreferredFood           INT (nullable)
PreferredPropertyType   INT (nullable)
PreferredFurnishing     INT (nullable)
MoveInDate              DATE

CreatedAt               TIMESTAMPTZ
UpdatedAt               TIMESTAMPTZ
```

**Indexes:**
- Unique index on UserId (one preference record per user)
- Index on PreferredCity
- Composite index on (BudgetMin, BudgetMax)

---

## Entity Relationships

```
User (1) ────────── (N) Properties
Property (1) ───────── (N) PropertyImages
Property (N) ───────── (N) Amenities (via PropertyAmenities)
User (1) ───────── (1) UserPreferences
```

---

## First-Time User Posting Flow

### Step 1: User Signs Up
```json
{
  "name": "Rahul Kumar",
  "email": "rahul@gmail.com",
  "phoneNumber": "9876543210",
  "gender": "Male",
  "password": "SecurePass123!"
}
```

### Step 2: User Posts Property
```json
{
  "title": "2BHK Flat available near Metro",
  "description": "Looking for a flatmate in a fully furnished flat",
  "propertyType": "TwoBhk",
  "rent": 12000,
  "deposit": 24000,
  "availableFrom": "2025-02-01",
  "city": "Bangalore",
  "area": "Whitefield",
  "address": "Near XYZ Tech Park, Whitefield",
  "latitude": 12.9698,
  "longitude": 77.7499,
  "preferredGender": "Any",
  "preferredFood": "Any",
  "furnishing": "Furnished"
}
```

### Step 3: Upload Images to Azure Blob Storage
```csharp
// Upload pattern
var blobPath = $"properties/{propertyId}/images/{imageId}.jpg";
var imageUrl = await _blobStorage.UploadAsync(file, blobPath);

// Save to DB
var propertyImage = new PropertyImage
{
    PropertyId = propertyId,
    ImageUrl = imageUrl,
    IsCover = isFirstImage,
    DisplayOrder = imageIndex
};
```

### Step 4: Assign Amenities
```json
{
  "propertyId": "uuid",
  "amenityIds": [
    "11111111-1111-1111-1111-111111111111",  // WiFi
    "22222222-2222-2222-2222-222222222222",  // AC
    "33333333-3333-3333-3333-333333333333"   // Washing Machine
  ]
}
```

---

## Search Query Examples

### 1. Search by City and Rent Range
```sql
SELECT * FROM "Properties"
WHERE "City" = 'Bangalore'
  AND "Rent" BETWEEN 10000 AND 15000
  AND "Status" = 1  -- Active
ORDER BY "CreatedAt" DESC;
```

### 2. Search with Filters
```sql
SELECT p.*, u."Name" as "OwnerName"
FROM "Properties" p
INNER JOIN "Users" u ON p."UserId" = u."Id"
WHERE p."City" = 'Bangalore'
  AND p."PropertyType" = 2  -- 2BHK
  AND p."PreferredGender" IN (1, 3)  -- Male or Any
  AND p."Furnishing" = 3  -- Furnished
  AND p."Status" = 1
ORDER BY p."Rent" ASC;
```

### 3. Get Property with Images and Amenities
```sql
SELECT 
    p.*,
    json_agg(DISTINCT pi.*) as "Images",
    json_agg(DISTINCT a."Name") as "Amenities"
FROM "Properties" p
LEFT JOIN "PropertyImages" pi ON p."Id" = pi."PropertyId"
LEFT JOIN "PropertyAmenities" pa ON p."Id" = pa."PropertyId"
LEFT JOIN "Amenities" a ON pa."AmenityId" = a."Id"
WHERE p."Id" = @propertyId
GROUP BY p."Id";
```

---

## Azure Resources Required

| Purpose | Azure Resource | Tier (MVP) |
|---------|---------------|------------|
| Database | Azure PostgreSQL Flexible Server | Basic (1-2 vCores) |
| File Storage | Azure Blob Storage (Hot tier) | Standard LRS |
| Cache (Future) | Azure Redis Cache | Basic C0 |
| Search (Future) | Azure Cognitive Search | Free |

---

## Scalability Path

### MVP (0-1K users)
- PostgreSQL Basic tier
- Blob Storage Standard
- Single region deployment

### Growth (1K-10K users)
- PostgreSQL Flexible Server (2-4 vCores)
- Enable read replicas
- Add Redis cache for hot data
- CDN for images

### Scale (10K+ users)
- PostgreSQL High Availability
- Azure Cognitive Search for full-text search
- Multi-region deployment
- Add message queue (Azure Service Bus)

---

## Migration Commands

### Create Migration
```bash
cd src/RoomMitra.Infrastructure
dotnet ef migrations add InitialPostgreSQLSchema --startup-project ../RoomMitra.Api
```

### Apply Migration
```bash
dotnet ef database update --startup-project ../RoomMitra.Api
```

### Generate SQL Script
```bash
dotnet ef migrations script --startup-project ../RoomMitra.Api --output migration.sql
```

---

## Connection String Example

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=roommitra-db.postgres.database.azure.com;Database=roommitra;Username=adminuser;Password=YourPassword123!;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## Next Steps

1. ✅ **Schema created** - All entities and configurations ready
2. ⏭️ **Create migration** - Generate EF Core migration
3. ⏭️ **Provision Azure PostgreSQL** - Create database in Azure
4. ⏭️ **Apply migration** - Run migration on Azure DB
5. ⏭️ **Test CRUD operations** - Verify all operations work
6. ⏭️ **Implement search** - Build search API with filters
7. ⏭️ **Add Azure Blob Storage** - Implement image upload

---

## Performance Optimizations

### Database Level
- ✅ Proper indexes on search columns
- ✅ Composite indexes for common queries
- ✅ JSONB for flexible data (PreferredAreas)
- Use connection pooling
- Enable query plan caching

### Application Level
- Implement pagination (skip/take)
- Use projection (select only needed columns)
- Cache frequently accessed data (Redis)
- Use async/await for all DB operations
- Implement response caching

---

## Security Best Practices

1. **Never store connection strings in code** - Use Azure Key Vault
2. **Enable SSL/TLS** - Always use encrypted connections
3. **Firewall rules** - Whitelist only your API server IPs
4. **Row-level security** - Users can only modify their own properties
5. **Backup strategy** - Daily automated backups
6. **Monitoring** - Enable Azure Monitor and Application Insights

---

## API Endpoints (Recommended)

```
POST   /api/auth/register          - User registration
POST   /api/auth/login             - User login
GET    /api/users/me               - Get current user profile

POST   /api/properties             - Create property listing
GET    /api/properties             - Search/list properties (with filters)
GET    /api/properties/{id}        - Get property details
PUT    /api/properties/{id}        - Update property
DELETE /api/properties/{id}        - Delete property

POST   /api/properties/{id}/images - Upload property images
DELETE /api/properties/{id}/images/{imageId} - Delete image

GET    /api/amenities              - Get all amenities
POST   /api/properties/{id}/amenities - Assign amenities to property

GET    /api/preferences            - Get user preferences
PUT    /api/preferences            - Update user preferences
```

---

## Summary

Your database schema is now production-ready with:

✅ Normalized relational design  
✅ Proper indexes for performance  
✅ Flexible amenities system  
✅ User preferences for matching  
✅ Azure Blob Storage integration ready  
✅ Scalable architecture  
✅ Strong typing with enums  
✅ Audit trails (CreatedAt/UpdatedAt)  
✅ Soft delete ready (Status field)  

You can now proceed to create and apply migrations! 🚀
