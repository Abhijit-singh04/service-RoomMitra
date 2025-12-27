# API Examples - Property Management

This document shows example API requests/responses for the RoomMitra property management system.

## Table of Contents
1. [User Registration & Authentication](#user-registration--authentication)
2. [Property Management](#property-management)
3. [Image Upload](#image-upload)
4. [Amenities Management](#amenities-management)
5. [Search & Filter](#search--filter)
6. [User Preferences](#user-preferences)

---

## User Registration & Authentication

### 1. Register New User
```http
POST /api/auth/register
Content-Type: application/json

{
  "name": "Rahul Kumar",
  "email": "rahul@gmail.com",
  "phoneNumber": "+919876543210",
  "password": "SecurePass123!",
  "gender": "Male"
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "userId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "message": "User registered successfully"
}
```

### 2. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "rahul@gmail.com",
  "password": "SecurePass123!"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "Rahul Kumar",
    "email": "rahul@gmail.com",
    "phoneNumber": "+919876543210",
    "gender": "Male",
    "isVerified": false
  }
}
```

---

## Property Management

### 1. Create Property Listing
```http
POST /api/properties
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Spacious 2BHK Flat near Metro Station",
  "description": "Looking for a clean, professional flatmate. Fully furnished 2BHK apartment in a premium society with all modern amenities. Walking distance from Whitefield Metro Station.",
  "propertyType": "TwoBhk",
  "rent": 12000,
  "deposit": 24000,
  "availableFrom": "2025-02-01",
  "city": "Bangalore",
  "area": "Whitefield",
  "address": "Building No 23, XYZ Tech Park Road, Whitefield",
  "latitude": 12.9698,
  "longitude": 77.7499,
  "preferredGender": "Any",
  "preferredFood": "Any",
  "furnishing": "Furnished",
  "amenityIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222",
    "33333333-3333-3333-3333-333333333333"
  ]
}
```

**Response (201 Created):**
```json
{
  "success": true,
  "propertyId": "prop-123-456-789",
  "message": "Property created successfully",
  "property": {
    "id": "prop-123-456-789",
    "title": "Spacious 2BHK Flat near Metro Station",
    "rent": 12000,
    "city": "Bangalore",
    "status": "Active",
    "createdAt": "2025-01-15T10:30:00Z"
  }
}
```

### 2. Get Property Details
```http
GET /api/properties/{propertyId}
```

**Response (200 OK):**
```json
{
  "id": "prop-123-456-789",
  "title": "Spacious 2BHK Flat near Metro Station",
  "description": "Looking for a clean, professional flatmate...",
  "propertyType": "TwoBhk",
  "rent": 12000,
  "deposit": 24000,
  "availableFrom": "2025-02-01",
  "city": "Bangalore",
  "area": "Whitefield",
  "address": "Building No 23, XYZ Tech Park Road, Whitefield",
  "latitude": 12.9698,
  "longitude": 77.7499,
  "preferredGender": "Any",
  "preferredFood": "Any",
  "furnishing": "Furnished",
  "status": "Active",
  "owner": {
    "id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
    "name": "Rahul Kumar",
    "phoneNumber": "+919876543210",
    "isVerified": false
  },
  "images": [
    {
      "id": "img-001",
      "imageUrl": "https://roommitra.blob.core.windows.net/properties/prop-123-456-789/images/img-001.jpg",
      "isCover": true,
      "displayOrder": 0
    },
    {
      "id": "img-002",
      "imageUrl": "https://roommitra.blob.core.windows.net/properties/prop-123-456-789/images/img-002.jpg",
      "isCover": false,
      "displayOrder": 1
    }
  ],
  "amenities": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "WiFi",
      "icon": "wifi"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Air Conditioning",
      "icon": "ac_unit"
    }
  ],
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-15T10:30:00Z"
}
```

### 3. Update Property
```http
PUT /api/properties/{propertyId}
Authorization: Bearer {token}
Content-Type: application/json

{
  "title": "Spacious 2BHK Flat near Metro Station - UPDATED",
  "rent": 11500,
  "description": "Updated description with new details..."
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Property updated successfully"
}
```

### 4. Delete Property
```http
DELETE /api/properties/{propertyId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Property deleted successfully"
}
```

### 5. Get My Properties
```http
GET /api/properties/my-listings
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "properties": [
    {
      "id": "prop-123-456-789",
      "title": "Spacious 2BHK Flat near Metro Station",
      "rent": 12000,
      "city": "Bangalore",
      "area": "Whitefield",
      "status": "Active",
      "coverImage": "https://...",
      "views": 45,
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ],
  "total": 1
}
```

---

## Image Upload

### 1. Upload Property Images
```http
POST /api/properties/{propertyId}/images
Authorization: Bearer {token}
Content-Type: multipart/form-data

files: [file1.jpg, file2.jpg, file3.jpg]
isCoverIndex: 0
```

**Response (200 OK):**
```json
{
  "success": true,
  "images": [
    {
      "id": "img-001",
      "imageUrl": "https://roommitra.blob.core.windows.net/properties/prop-123-456-789/images/img-001.jpg",
      "isCover": true,
      "displayOrder": 0
    },
    {
      "id": "img-002",
      "imageUrl": "https://roommitra.blob.core.windows.net/properties/prop-123-456-789/images/img-002.jpg",
      "isCover": false,
      "displayOrder": 1
    }
  ]
}
```

### 2. Delete Image
```http
DELETE /api/properties/{propertyId}/images/{imageId}
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Image deleted successfully"
}
```

### 3. Set Cover Image
```http
PUT /api/properties/{propertyId}/images/{imageId}/set-cover
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Cover image updated successfully"
}
```

---

## Amenities Management

### 1. Get All Amenities
```http
GET /api/amenities
```

**Response (200 OK):**
```json
{
  "amenities": [
    {
      "id": "11111111-1111-1111-1111-111111111111",
      "name": "WiFi",
      "description": "High-speed internet connection",
      "icon": "wifi"
    },
    {
      "id": "22222222-2222-2222-2222-222222222222",
      "name": "Air Conditioning",
      "description": "AC in rooms",
      "icon": "ac_unit"
    }
  ]
}
```

### 2. Assign Amenities to Property
```http
POST /api/properties/{propertyId}/amenities
Authorization: Bearer {token}
Content-Type: application/json

{
  "amenityIds": [
    "11111111-1111-1111-1111-111111111111",
    "22222222-2222-2222-2222-222222222222"
  ]
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Amenities assigned successfully"
}
```

---

## Search & Filter

### 1. Basic Search
```http
GET /api/properties/search?city=Bangalore&page=1&pageSize=20
```

### 2. Advanced Search with Filters
```http
GET /api/properties/search?city=Bangalore&minRent=10000&maxRent=15000&propertyType=TwoBhk&furnishing=Furnished&preferredGender=Any&page=1&pageSize=20
```

**Response (200 OK):**
```json
{
  "properties": [
    {
      "id": "prop-123-456-789",
      "title": "Spacious 2BHK Flat near Metro Station",
      "rent": 12000,
      "deposit": 24000,
      "city": "Bangalore",
      "area": "Whitefield",
      "propertyType": "TwoBhk",
      "furnishing": "Furnished",
      "coverImage": "https://...",
      "owner": {
        "name": "Rahul Kumar",
        "isVerified": false
      },
      "amenitiesCount": 5,
      "imagesCount": 3,
      "createdAt": "2025-01-15T10:30:00Z"
    }
  ],
  "pagination": {
    "currentPage": 1,
    "pageSize": 20,
    "totalCount": 45,
    "totalPages": 3
  }
}
```

### 3. Search by Area
```http
GET /api/properties/search?city=Bangalore&areas=Whitefield,Marathahalli,Koramangala
```

### 4. Search Available From Date
```http
GET /api/properties/search?city=Bangalore&availableFrom=2025-02-01
```

---

## User Preferences

### 1. Set/Update User Preferences
```http
PUT /api/preferences
Authorization: Bearer {token}
Content-Type: application/json

{
  "budgetMin": 10000,
  "budgetMax": 15000,
  "preferredCity": "Bangalore",
  "preferredAreas": ["Whitefield", "Marathahalli"],
  "preferredGender": "Any",
  "preferredFood": "Vegetarian",
  "preferredPropertyType": "TwoBhk",
  "preferredFurnishing": "Furnished",
  "moveInDate": "2025-02-15"
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "message": "Preferences saved successfully"
}
```

### 2. Get User Preferences
```http
GET /api/preferences
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "id": "pref-123",
  "budgetMin": 10000,
  "budgetMax": 15000,
  "preferredCity": "Bangalore",
  "preferredAreas": ["Whitefield", "Marathahalli"],
  "preferredGender": "Any",
  "preferredFood": "Vegetarian",
  "preferredPropertyType": "TwoBhk",
  "preferredFurnishing": "Furnished",
  "moveInDate": "2025-02-15",
  "createdAt": "2025-01-15T10:30:00Z",
  "updatedAt": "2025-01-16T14:20:00Z"
}
```

### 3. Get Matching Properties (Based on Preferences)
```http
GET /api/properties/matches
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "matches": [
    {
      "id": "prop-123-456-789",
      "title": "Spacious 2BHK Flat near Metro Station",
      "rent": 12000,
      "matchScore": 95,
      "matchReasons": [
        "Rent within budget",
        "Preferred area: Whitefield",
        "Preferred property type: 2BHK",
        "Fully furnished"
      ],
      "coverImage": "https://..."
    }
  ],
  "total": 5
}
```

---

## Error Responses

### 400 Bad Request
```json
{
  "success": false,
  "errors": {
    "Rent": ["Rent must be greater than 0"],
    "City": ["City is required"]
  }
}
```

### 401 Unauthorized
```json
{
  "success": false,
  "message": "Unauthorized. Please login."
}
```

### 403 Forbidden
```json
{
  "success": false,
  "message": "You don't have permission to perform this action"
}
```

### 404 Not Found
```json
{
  "success": false,
  "message": "Property not found"
}
```

### 500 Internal Server Error
```json
{
  "success": false,
  "message": "An error occurred while processing your request",
  "requestId": "req-abc-123"
}
```

---

## Sample cURL Commands

### Register User
```bash
curl -X POST https://api.roommitra.com/api/auth/register \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Rahul Kumar",
    "email": "rahul@gmail.com",
    "phoneNumber": "+919876543210",
    "password": "SecurePass123!",
    "gender": "Male"
  }'
```

### Create Property
```bash
curl -X POST https://api.roommitra.com/api/properties \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Spacious 2BHK Flat",
    "rent": 12000,
    "city": "Bangalore"
  }'
```

### Search Properties
```bash
curl "https://api.roommitra.com/api/properties/search?city=Bangalore&minRent=10000&maxRent=15000"
```

---

## Testing with VS Code REST Client

Create a file `api-tests.http` in your project:

```http
### Variables
@baseUrl = https://localhost:7000/api
@token = YOUR_JWT_TOKEN

### Register User
POST {{baseUrl}}/auth/register
Content-Type: application/json

{
  "name": "Rahul Kumar",
  "email": "rahul@gmail.com",
  "password": "SecurePass123!"
}

### Login
POST {{baseUrl}}/auth/login
Content-Type: application/json

{
  "email": "rahul@gmail.com",
  "password": "SecurePass123!"
}

### Create Property
POST {{baseUrl}}/properties
Authorization: Bearer {{token}}
Content-Type: application/json

{
  "title": "2BHK Flat",
  "rent": 12000,
  "city": "Bangalore"
}

### Search Properties
GET {{baseUrl}}/properties/search?city=Bangalore
```

---

## Next Steps

1. ✅ Schema created
2. ⏭️ Implement these API endpoints in controllers
3. ⏭️ Add validation using FluentValidation
4. ⏭️ Implement Azure Blob Storage for images
5. ⏭️ Add pagination helpers
6. ⏭️ Implement matching algorithm
7. ⏭️ Add caching layer
8. ⏭️ Write integration tests
