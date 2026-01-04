# RoomMitra Authentication Flow

This document outlines the authentication strategy for RoomMitra, combining Google (Azure AD B2C) sign-up with Phone OTP sign-up.

---

## Overview

| Method | Sign Up | Login | Phone Verified | Can Post Listings |
|--------|---------|-------|----------------|-------------------|
| **Google (Azure AD)** | ✅ Primary | ✅ 1-click | ❌ No | ❌ No (need phone) |
| **Phone OTP** | ✅ Alternative | ✅ Available | ✅ Yes | ✅ Yes |

---

## Sign-Up Flow

### Option 1: Google Sign-Up (Recommended)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         GOOGLE SIGN-UP FLOW                             │
└─────────────────────────────────────────────────────────────────────────┘

     User clicks "Continue with Google"
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  Google OAuth Consent               │
     │  • User approves access             │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  Auto-filled from Google:           │
     │  ✓ Name: John Doe                   │
     │  ✓ Email: john@gmail.com            │
     │  ✓ Profile Photo                    │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  Phone Verification Required        │
     │                                     │
     │  "Add your phone number to          │
     │   complete sign-up"                 │
     │                                     │
     │  ┌───────────────────────────────┐  │
     │  │ +91 | Enter phone number      │  │
     │  └───────────────────────────────┘  │
     │                                     │
     │  [Send OTP]                         │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  OTP Verification                   │
     │                                     │
     │  Enter the 6-digit code sent to     │
     │  +91 98XXXXXXXX                     │
     │                                     │
     │  ┌───┬───┬───┬───┬───┬───┐          │
     │  │   │   │   │   │   │   │          │
     │  └───┴───┴───┴───┴───┴───┘          │
     │                                     │
     │  [Verify & Complete Sign Up]        │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  ✅ Account Created                 │
     │                                     │
     │  → Redirect to Dashboard            │
     └─────────────────────────────────────┘
```

**Benefits:**
- Name, email, and photo auto-filled from Google
- User only needs to add phone number
- Lower friction, higher conversion

---

### Option 2: Phone OTP Sign-Up

```
┌─────────────────────────────────────────────────────────────────────────┐
│                         PHONE OTP SIGN-UP FLOW                          │
└─────────────────────────────────────────────────────────────────────────┘

     User clicks "Continue with Phone Number"
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  Enter Phone Number                 │
     │                                     │
     │  ┌───────────────────────────────┐  │
     │  │ +91 | Enter phone number      │  │
     │  └───────────────────────────────┘  │
     │                                     │
     │  [Send OTP]                         │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  OTP Verification                   │
     │                                     │
     │  ┌───┬───┬───┬───┬───┬───┐          │
     │  │   │   │   │   │   │   │          │
     │  └───┴───┴───┴───┴───┴───┘          │
     │                                     │
     │  [Verify]                           │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  Complete Your Profile              │
     │                                     │
     │  Name *                             │
     │  ┌───────────────────────────────┐  │
     │  │                               │  │
     │  └───────────────────────────────┘  │
     │                                     │
     │  Email (for notifications)          │
     │  ┌───────────────────────────────┐  │
     │  │                               │  │
     │  └───────────────────────────────┘  │
     │                                     │
     │  [Complete Sign Up]                 │
     └─────────────────────────────────────┘
                    │
                    ▼
     ┌─────────────────────────────────────┐
     │  ✅ Account Created                 │
     │                                     │
     │  → Redirect to Dashboard            │
     └─────────────────────────────────────┘
```

**When to use:**
- Users without Google accounts
- Users who prefer phone-first authentication
- Common pattern in Indian market

---

## Login Flow

### Returning Users

```
┌─────────────────────────────────────────────────────────────────────────┐
│                            LOGIN FLOW                                   │
└─────────────────────────────────────────────────────────────────────────┘

                     User visits Login page
                            │
            ┌───────────────┴───────────────┐
            │                               │
            ▼                               ▼
   ┌─────────────────┐             ┌─────────────────┐
   │ Continue with   │             │ Continue with   │
   │ Google          │             │ Phone Number    │
   │                 │             │                 │
   │ [Google Icon]   │             │ [Phone Icon]    │
   └────────┬────────┘             └────────┬────────┘
            │                               │
            │                               ▼
            │                      ┌─────────────────┐
            │                      │ Enter Phone     │
            │                      │ +91 98XXXXXXXX  │
            │                      └────────┬────────┘
            │                               │
            │                               ▼
            │                      ┌─────────────────┐
            │                      │ Verify OTP      │
            │                      │ [_ _ _ _ _ _]   │
            │                      └────────┬────────┘
            │                               │
            └───────────────┬───────────────┘
                            │
                            ▼
                   ┌─────────────────┐
                   │   Dashboard     │
                   └─────────────────┘
```

---

## Phone Verification Rules

| Action | Phone Verified Required? |
|--------|--------------------------|
| Browse listings | ❌ No |
| View listing details | ❌ No |
| Save favorites | ❌ No |
| Post a listing | ✅ Yes |
| Contact landlord/flatmate | ✅ Yes |
| Update profile | ❌ No |

---

## User Data Model

After sign-up (either method), the user record contains:

```typescript
interface User {
  id: string;                                    // UUID (matches Azure AD oid if Google signup)
  name: string;                                  // From Google OR user input
  email: string;                                 // From Google OR user input
  phone: string;                                 // Always verified via OTP
  phoneVerified: boolean;                        // true (required)
  profileImage?: string;                         // From Google OR uploaded
  bio?: string;
  occupation?: string;
  authProvider: 'google' | 'phone';              // Primary auth method
  createdAt: string;
  updatedAt: string;
}
```

---

## Data Synchronization

### Single Source of Truth: PostgreSQL

```
┌─────────────────────────────────────────────────────────────────────────┐
│                     DATA SYNCHRONIZATION                                │
└─────────────────────────────────────────────────────────────────────────┘

   Azure AD B2C (Google)                Azure Communication Service
   (Authentication)                     (OTP Verification)
        │                                       │
        │ JWT Token with claims                 │ SMS OTP
        │ • oid (object ID)                     │
        │ • email                               │
        │ • name                                │
        │                                       │
        ▼                                       ▼
   ┌─────────────────────────────────────────────────────────────────┐
   │                        RoomMitra API                            │
   │                                                                  │
   │   UserSyncMiddleware:                                           │
   │   • Extract user info from JWT                                  │
   │   • Create/Update user in PostgreSQL                            │
   │   • Sync phone verification status                              │
   │                                                                  │
   └─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
   ┌─────────────────────────────────────────────────────────────────┐
   │                    PostgreSQL (AppUser)                         │
   │                                                                  │
   │   • Id = Azure AD oid (for Google users)                        │
   │   • Email, Name (from Azure AD)                                 │
   │   • PhoneNumber, PhoneVerified (from OTP service)               │
   │   • Bio, Occupation, etc. (user profile data)                   │
   │                                                                  │
   │   ══════════════════════════════════════════════                │
   │   THIS IS THE SINGLE SOURCE OF TRUTH                            │
   │   ══════════════════════════════════════════════                │
   └─────────────────────────────────────────────────────────────────┘
```

---

## UI Components

### Sign-Up Screen

```
┌─────────────────────────────────────────────────────────────────────────┐
│                                                                         │
│                        🏠 RoomMitra                                     │
│                                                                         │
│                     Find your perfect flatmate                          │
│                                                                         │
│         ┌─────────────────────────────────────────────────┐             │
│         │  🔵  Continue with Google                       │  ← Primary  │
│         └─────────────────────────────────────────────────┘             │
│                                                                         │
│                            ── or ──                                     │
│                                                                         │
│         ┌─────────────────────────────────────────────────┐             │
│         │  📱  Continue with Phone Number                 │             │
│         └─────────────────────────────────────────────────┘             │
│                                                                         │
│                                                                         │
│         Already have an account? Sign in                                │
│                                                                         │
│         ─────────────────────────────────────────────────               │
│         By continuing, you agree to our Terms of Service                │
│         and Privacy Policy                                              │
│                                                                         │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## Error Handling

| Scenario | Action |
|----------|--------|
| Google OAuth fails | Show error, offer phone sign-up |
| OTP delivery fails | Show retry option, check phone format |
| OTP expired | Allow resend after 30s cooldown |
| Phone already registered | Prompt to login instead |
| Google account already linked | Prompt to login instead |

---

## Security Considerations

1. **OTP Rate Limiting**: Max 3 OTP requests per phone per hour
2. **OTP Expiry**: 5 minutes
3. **Failed Attempts**: Lock after 5 failed OTP attempts (15-minute cooldown)
4. **Phone Format**: Validate Indian phone numbers (+91)
5. **Session**: JWT tokens with 24-hour expiry, refresh tokens for 7 days

---

## API Endpoints

### Phone OTP Flow

#### 1. Request OTP
```http
POST /api/auth/request-otp
Content-Type: application/json

{
  "phoneNumber": "+919812345678"
}
```

**Response:**
```json
{
  "requestId": "abc123..."
}
```

#### 2. Verify OTP
```http
POST /api/auth/verify-otp
Content-Type: application/json

{
  "phoneNumber": "+919812345678",
  "requestId": "abc123...",
  "code": "123456"
}
```

**Response:**
```json
{
  "accessToken": "eyJ...",
  "user": {
    "id": "uuid",
    "name": "",
    "email": "",
    "profileImageUrl": null,
    "phoneNumber": "+919812345678",
    "phoneVerified": true,
    "isVerified": true,
    "isProfileComplete": false,
    "authProvider": "phone"
  },
  "isNewUser": true,
  "requiresProfileCompletion": true
}
```

#### 3. Complete Profile (for Phone OTP users)
```http
POST /api/auth/complete-profile
Authorization: Bearer {accessToken}
Content-Type: application/json

{
  "name": "John Doe",
  "email": "john@example.com",
  "occupation": "Software Engineer",
  "bio": "Looking for a flatmate in Bangalore"
}
```

**Response:**
```json
{
  "accessToken": "eyJ...",
  "user": {
    "id": "uuid",
    "name": "John Doe",
    "email": "john@example.com",
    "profileImageUrl": null,
    "phoneNumber": "+919812345678",
    "phoneVerified": true,
    "isVerified": true,
    "isProfileComplete": true,
    "authProvider": "phone"
  },
  "isNewUser": false,
  "requiresProfileCompletion": false
}
```

### Google (Azure AD) Flow

#### Sync External User (after OAuth callback)
```http
POST /api/auth/external/sync
Content-Type: application/json

{
  "objectId": "azure-ad-oid",
  "email": "john@gmail.com",
  "name": "John Doe",
  "profileImageUrl": "https://...",
  "identityProvider": "google"
}
```

**Response:**
```json
{
  "accessToken": "eyJ...",
  "user": {
    "id": "uuid",
    "name": "John Doe",
    "email": "john@gmail.com",
    "profileImageUrl": "https://...",
    "phoneNumber": null,
    "phoneVerified": false,
    "isVerified": false,
    "isProfileComplete": true,
    "authProvider": "google"
  },
  "isNewUser": true,
  "requiresProfileCompletion": false
}
```

---

## Implementation Status

- [x] Phone OTP request endpoint
- [x] Phone OTP verification endpoint
- [x] Profile completion endpoint
- [x] External user sync endpoint
- [x] AppUser entity with AuthProvider, ExternalId, IsProfileComplete
- [x] AuthUserDto with full user info
- [x] AuthResponse with isNewUser and requiresProfileCompletion flags
- [ ] Azure AD B2C configuration for Google OAuth
- [ ] Azure Communication Service / Firebase for OTP (SMS sender)
- [ ] Phone verification UI components (frontend)
- [ ] Profile completion UI flow (frontend)
- [ ] Protected routes for verified users (frontend)
