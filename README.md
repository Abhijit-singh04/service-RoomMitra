# service-RoomMitra (C# Backend)

Clean, modular backend scaffold for RoomMitra.

## Folder Layout

- `src/RoomMitra.Api` — ASP.NET Core Web API (HTTP, auth middleware, controllers)
- `src/RoomMitra.Application` — use-cases, DTOs, and *interfaces* the app needs
- `src/RoomMitra.Domain` — core entities/enums (no framework dependencies)
- `src/RoomMitra.Infrastructure` — implementations (EF Core, Identity, Azure Blob)

## What Each Layer Does

### Domain

Purpose: the “business core”.

- Entities and enums that represent your product concepts (e.g. `FlatListing`, `ListingStatus`).
- No references to ASP.NET, EF Core, Azure SDK, etc.

### Application

Purpose: the “rules and use-cases” of the app.

- Defines abstractions like repositories/services (`IFlatListingRepository`, `IAuthService`, `IBlobStorage`).
- Contains orchestration services like `ListingsService` that implement the product actions.
- Returns DTOs designed for API responses (`FlatListingSummaryDto`, `FlatListingDetailDto`).

Key rule: Application depends only on Domain (and small BCL packages).

### Infrastructure

Purpose: the “adapters” that talk to the outside world.

- EF Core + SQL Server: `RoomMitraDbContext`, entity configurations, repository implementation.
- Identity: user store + password hashing + sign-in checks.
- Azure Blob Storage: upload implementation returning a public URL.

Key rule: Infrastructure implements Application interfaces, but Application does not know Infrastructure exists.

### API

Purpose: the “delivery mechanism” (HTTP).

- Controllers + request/response shaping.
- JWT bearer authentication + authorization.
- CORS + Swagger.
- Wires everything together using DI.

## Dependency Direction (Who References Whom)

This repo enforces a one-way dependency flow:

- `RoomMitra.Domain` → (depends on nothing)
- `RoomMitra.Application` → depends on `RoomMitra.Domain`
- `RoomMitra.Infrastructure` → depends on `RoomMitra.Application` and `RoomMitra.Domain`
- `RoomMitra.Api` → depends on all three (to compose and host the app)

In short: **dependencies point inward** (Domain is at the center).

## How The Layers Connect At Runtime

The connection happens via Dependency Injection in the API.

1. API registers Application services:
	- `services.AddApplication()` (from `RoomMitra.Application/DependencyInjection.cs`)
2. API registers Infrastructure implementations:
	- `services.AddInfrastructure(configuration)` (from `RoomMitra.Infrastructure/DependencyInjection.cs`)
3. Controllers depend only on Application abstractions/services:
	- Example: `ListingsController` depends on `IListingsService`, not EF Core.

So the flow looks like:

HTTP request → Controller (API) → Service (Application) → Repository/Blob/Auth impl (Infrastructure) → Azure SQL / Azure Blob

## Configuration

Update `src/RoomMitra.Api/appsettings.json`:

- `ConnectionStrings:DefaultConnection` (Azure SQL)
- `Jwt` (Issuer/Audience/SigningKey)
- `AzureBlob` (ConnectionString/ContainerName/PublicBaseUrl)

## Local Run

From `service-RoomMitra/`:

- Build: `dotnet build RoomMitra.sln -c Release`
- Run API: `dotnet run --project src/RoomMitra.Api/RoomMitra.Api.csproj --urls http://localhost:5055`
- Swagger UI: `http://localhost:5055/swagger`

## API Surface (MVP)

Health:

- `GET /api/health`
- `GET /api/health/live`
- `GET /api/health/ready`

Auth:

- `POST /api/auth/register`
- `POST /api/auth/login`

Listings:

- `GET /api/listings` (filters + pagination)
- `GET /api/listings/{id}`
- `POST /api/listings` (auth required)
- `PUT /api/listings/{id}` (auth required, owner only)
- `PATCH /api/listings/{id}/status` (auth required, owner only)
- `DELETE /api/listings/{id}` (auth required, owner only)

Uploads:

- `POST /api/uploads/images` (auth required, image only, <= 5MB)
