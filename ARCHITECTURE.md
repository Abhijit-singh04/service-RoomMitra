# RoomMitra Database Architecture

## Entity Relationship Diagram (ERD)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         ROOMMITRA DATABASE SCHEMA                        │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────┐
│    AspNetUsers       │
│ (Users/Identity)     │
├──────────────────────┤
│ • Id (PK)            │◄─────┐
│ • Name               │      │
│ • Email (UNIQUE)     │      │
│ • PhoneNumber        │      │  1
│ • Gender             │      │
│ • IsVerified         │      │
│ • ProfileImageUrl    │      │
│ • Occupation         │      │
│ • Bio                │      │
│ • CreatedAt          │      │
│ • UpdatedAt          │      │
└──────────────────────┘      │
                              │
                              │
                              │  N
                    ┌─────────┴────────────┐
                    │                      │
            ┌───────┴──────────┐  ┌────────▼─────────┐
            │   Properties     │  │ UserPreferences  │
            ├──────────────────┤  ├──────────────────┤
            │ • Id (PK)        │  │ • Id (PK)        │
            │ • UserId (FK) ───┘  │ • UserId (FK)    │
            │ • Title          │  │ • BudgetMin      │
            │ • Description    │  │ • BudgetMax      │
            │ • PropertyType   │  │ • PreferredCity  │
            │ • Rent           │  │ • PreferredAreas │
            │ • Deposit        │  │ • PreferredGender│
            │ • AvailableFrom  │  │ • PreferredFood  │
            │ • City           │  │ • MoveInDate     │
            │ • Area           │  │ • CreatedAt      │
            │ • Address        │  │ • UpdatedAt      │
            │ • Latitude       │  └──────────────────┘
            │ • Longitude      │
            │ • PreferredGender│
            │ • PreferredFood  │
            │ • Furnishing     │
            │ • Status         │
            │ • CreatedAt      │
            │ • UpdatedAt      │
            └──────┬───────────┘
                   │
                   │ 1
                   │
                   │
                   │ N
         ┌─────────┴──────────────┐
         │                        │
  ┌──────▼─────────┐    ┌─────────▼──────────┐
  │ PropertyImages │    │ PropertyAmenities  │
  ├────────────────┤    │   (Join Table)     │
  │ • Id (PK)      │    ├────────────────────┤
  │ • PropertyId(FK)│   │ • PropertyId (FK)  │──┐
  │ • ImageUrl     │    │ • AmenityId (FK)   │  │
  │ • IsCover      │    │ • CreatedAt        │  │
  │ • DisplayOrder │    └────────────────────┘  │
  │ • CreatedAt    │                            │
  │ • UpdatedAt    │                            │
  └────────────────┘                            │
                                                │
                                                │ N
                                                │
                                                │
                                                │ 1
                                        ┌───────▼────────┐
                                        │   Amenities    │
                                        ├────────────────┤
                                        │ • Id (PK)      │
                                        │ • Name (UNIQUE)│
                                        │ • Description  │
                                        │ • Icon         │
                                        └────────────────┘
```

---

## Relationship Summary

| Relationship | Cardinality | Description |
|--------------|-------------|-------------|
| User → Properties | 1:N | One user can have multiple property listings |
| User → UserPreferences | 1:1 | Each user has one set of preferences |
| Property → PropertyImages | 1:N | One property can have multiple images |
| Property ↔ Amenities | N:N | Many properties can have many amenities (via PropertyAmenities) |

---

## Data Flow Diagram

### 1. User Posts a Property

```
┌──────────┐
│  User    │
│ (Client) │
└────┬─────┘
     │
     │ 1. POST /api/properties
     │    {title, rent, city, ...}
     ▼
┌────────────────┐
│   API Layer    │
│  (Controller)  │
└────┬───────────┘
     │
     │ 2. Validate & Map to Entity
     ▼
┌──────────────────┐
│ Business Logic   │
│   (Service)      │
└────┬─────────────┘
     │
     │ 3. Save Property
     ▼
┌──────────────────┐      ┌──────────────────┐
│   Properties     │      │  Azure Blob      │
│   (Database)     │      │   Storage        │
└──────────────────┘      └──────────────────┘
     ▲                              ▲
     │ 4. Save metadata             │
     │                              │ 5. Upload images
     │                              │
     └──────────────────────────────┘
```

### 2. User Searches for Properties

```
┌──────────┐
│  User    │
│ (Client) │
└────┬─────┘
     │
     │ GET /api/properties/search
     │ ?city=Bangalore&minRent=10000&maxRent=15000
     ▼
┌────────────────┐
│   API Layer    │
│  (Controller)  │
└────┬───────────┘
     │
     │ Build query
     ▼
┌──────────────────┐
│ PostgreSQL Query │
│                  │
│ SELECT p.*,      │
│   u.Name,        │
│   Images,        │
│   Amenities      │
│ FROM Properties  │
│ WHERE            │
│   City = 'Bng'   │
│   AND Rent       │
│   BETWEEN ...    │
└────┬─────────────┘
     │
     │ Return results
     ▼
┌──────────────────┐
│  JSON Response   │
│                  │
│ {                │
│   properties: [] │
│   pagination: {} │
│ }                │
└──────────────────┘
```

---

## Indexing Strategy

### Primary Indexes (Automatically Created)
- `Properties.Id` (Primary Key, UUID)
- `PropertyImages.Id` (Primary Key, UUID)
- `Amenities.Id` (Primary Key, UUID)
- `UserPreferences.Id` (Primary Key, UUID)

### Foreign Key Indexes
- `Properties.UserId` → Links to Users
- `PropertyImages.PropertyId` → Links to Properties
- `PropertyAmenities.PropertyId` → Links to Properties
- `PropertyAmenities.AmenityId` → Links to Amenities

### Search Optimization Indexes
```sql
-- City search
CREATE INDEX IX_Properties_City ON Properties (City);

-- Rent range search
CREATE INDEX IX_Properties_Rent ON Properties (Rent);

-- Property type filter
CREATE INDEX IX_Properties_PropertyType ON Properties (PropertyType);

-- Status filter (Active/Inactive)
CREATE INDEX IX_Properties_Status ON Properties (Status);

-- Date availability
CREATE INDEX IX_Properties_AvailableFrom ON Properties (AvailableFrom);

-- Composite index for common queries
CREATE INDEX IX_Properties_Search 
ON Properties (City, Rent, Status);
```

---

## Storage Strategy

### Database (PostgreSQL)
**Purpose:** Structured relational data
- User accounts
- Property listings
- Amenities
- Relationships

**Size Estimate:**
- 1000 properties ≈ 5-10 MB
- 10,000 properties ≈ 50-100 MB

---

### Blob Storage (Azure Blob Storage)
**Purpose:** Images and media files

**Structure:**
```
roommitra-container/
├── properties/
│   ├── {propertyId}/
│   │   ├── images/
│   │   │   ├── {imageId-1}.jpg
│   │   │   ├── {imageId-2}.jpg
│   │   │   └── {imageId-3}.jpg
│   │   └── documents/
│   │       └── agreement.pdf
│   └── ...
├── users/
│   └── {userId}/
│       ├── profile.jpg
│       └── documents/
│           └── id-proof.jpg
└── ...
```

**Size Estimate:**
- 1 image ≈ 1-3 MB
- 5 images per property = 5-15 MB
- 1000 properties = 5-15 GB

---

### Cache (Future - Azure Redis)
**Purpose:** Frequently accessed data
- Search results
- Popular listings
- User sessions
- API responses

**What to Cache:**
```
Key Pattern                      | TTL    | Data
--------------------------------|--------|------------------
search:bangalore:10k-15k        | 5 min  | Search results
property:{id}                   | 1 hour | Property details
amenities:all                   | 1 day  | All amenities
user:{id}:preferences           | 1 hour | User preferences
```

---

## Scaling Considerations

### Current Schema (MVP: 0-10K users)
✅ PostgreSQL Basic tier  
✅ Single database instance  
✅ All queries direct to DB  
✅ No caching layer  

### Growing (10K-100K users)
⏭️ PostgreSQL Flexible Server (4-8 vCores)  
⏭️ Add Redis cache for hot data  
⏭️ Enable read replicas  
⏭️ CDN for images  

### Scale (100K+ users)
⏭️ PostgreSQL High Availability  
⏭️ Database sharding (by city)  
⏭️ Elasticsearch for search  
⏭️ Message queue for async operations  
⏭️ Multi-region deployment  

---

## Query Performance Examples

### Slow Query (BAD ❌)
```sql
-- No indexes, full table scan
SELECT * FROM Properties
WHERE Description LIKE '%furnished%'
  AND Rent < 20000;
```

### Optimized Query (GOOD ✅)
```sql
-- Using indexes and specific columns
SELECT 
    Id, Title, Rent, City, Area,
    (SELECT ImageUrl FROM PropertyImages 
     WHERE PropertyId = p.Id AND IsCover = TRUE 
     LIMIT 1) AS CoverImage
FROM Properties p
WHERE City = 'Bangalore'
  AND Rent BETWEEN 10000 AND 15000
  AND Status = 1
ORDER BY CreatedAt DESC
LIMIT 20;
```

### Batch Loading (EFFICIENT 🚀)
```sql
-- Load property with images and amenities in one query
SELECT 
    p.*,
    json_agg(DISTINCT pi.*) AS Images,
    json_agg(DISTINCT a.Name) AS Amenities
FROM Properties p
LEFT JOIN PropertyImages pi ON p.Id = pi.PropertyId
LEFT JOIN PropertyAmenities pa ON p.Id = pa.PropertyId
LEFT JOIN Amenities a ON pa.AmenityId = a.Id
WHERE p.Id = @propertyId
GROUP BY p.Id;
```

---

## Security Layers

```
┌─────────────────────────────────────────┐
│         Client (Web/Mobile)             │
└─────────────────┬───────────────────────┘
                  │
                  │ HTTPS/TLS
                  ▼
┌─────────────────────────────────────────┐
│      Azure Front Door / CDN             │
│      (DDoS protection, WAF)             │
└─────────────────┬───────────────────────┘
                  │
                  │ Firewall Rules
                  ▼
┌─────────────────────────────────────────┐
│       API Gateway (Optional)            │
│       (Rate limiting, throttling)       │
└─────────────────┬───────────────────────┘
                  │
                  │ JWT Authentication
                  ▼
┌─────────────────────────────────────────┐
│          .NET API Server                │
│      (Authorization, Validation)        │
└─────────────────┬───────────────────────┘
                  │
                  │ Connection Pool
                  │ SSL/TLS Required
                  ▼
┌─────────────────────────────────────────┐
│    Azure PostgreSQL Flexible Server    │
│    (Firewall, VNet, Private Link)      │
└─────────────────────────────────────────┘
```

---

## Backup & Recovery Strategy

### Automated Backups
```
Daily:    Full database backup (retained 7 days)
Weekly:   Full backup + transaction logs (retained 30 days)
Monthly:  Full backup (retained 12 months)
```

### Point-in-Time Recovery
```
Restore to any point in time within retention period
Typical retention: 7-35 days
```

### Disaster Recovery
```
Primary Region:     East US
Secondary Region:   West US (Read replica)
RTO (Recovery Time): < 1 hour
RPO (Data Loss):     < 5 minutes
```

---

## Monitoring Metrics

### Database Health
- Connection count
- Active queries
- Query execution time
- Deadlocks
- Cache hit ratio
- Database size growth

### Application Metrics
- API response time
- Request rate (requests/sec)
- Error rate
- User registrations
- Property listings created
- Search queries

### Business Metrics
- Daily active users
- Properties posted per day
- Search queries per day
- Conversion rate (views → contact)

---

This architecture is designed to be:
✅ **Scalable** - Can handle growth from 100 to 100K+ users  
✅ **Performant** - Optimized queries and indexes  
✅ **Secure** - Multiple layers of security  
✅ **Reliable** - Automated backups and monitoring  
✅ **Cost-effective** - Start small, scale as needed  
