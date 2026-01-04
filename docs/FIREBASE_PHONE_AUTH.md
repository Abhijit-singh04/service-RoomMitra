# Firebase Phone OTP Authentication

This document describes the Firebase Phone OTP authentication flow implemented as an independent auth path alongside the existing Google/Gmail/Azure AD login flows.

## Overview

Firebase Phone Authentication provides a secure way for users to sign up and login using their mobile phone number with OTP (One-Time Password) verification.

### Key Features

1. **Signup Flow**: Phone OTP → Create username/password
2. **Login Flow**: Username + Password authentication
3. **Forgot Password**: Phone OTP → Reset password

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         Frontend (Next.js)                       │
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │  Firebase Client SDK │    │    API Service Functions        │ │
│  │  - sendOtp()        │    │    - verifyPhone()              │ │
│  │  - verifyOtp()      │    │    - register()                 │ │
│  │  - getIdToken()     │    │    - loginWithUsername()        │ │
│  └─────────────────────┘    │    - resetPassword()            │ │
│                             └─────────────────────────────────┘ │
└────────────────────────────────────┬────────────────────────────┘
                                     │ Firebase ID Token
                                     ▼
┌─────────────────────────────────────────────────────────────────┐
│                         Backend (C# .NET)                        │
│  ┌─────────────────────┐    ┌─────────────────────────────────┐ │
│  │  FirebaseAuthService │    │  FirebasePhoneAuthService       │ │
│  │  - VerifyIdToken()   │    │  - VerifyPhoneAsync()           │ │
│  └─────────────────────┘    │  - RegisterWithPhoneAsync()      │ │
│                             │  - LoginWithUsernameAsync()      │ │
│                             │  - ResetPasswordAsync()          │ │
│                             └─────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────┘
```

## Frontend Implementation

### Files Created/Modified

| File | Purpose |
|------|---------|
| `src/lib/firebase-config.ts` | Firebase configuration |
| `src/lib/firebase-phone-auth.ts` | Firebase SDK wrapper functions |
| `src/contexts/firebase-phone-auth-context.tsx` | React context for auth state |
| `src/app/auth/phone-signup/page.tsx` | Phone signup page |
| `src/app/auth/phone-login/page.tsx` | Username/password login page |
| `src/app/auth/forgot-password/page.tsx` | Password reset page |
| `src/lib/api.ts` | API service functions (extended) |

### Environment Variables

Add to `.env.local`:

```env
# Firebase Configuration
NEXT_PUBLIC_FIREBASE_API_KEY=your-api-key
NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN=your-project.firebaseapp.com
NEXT_PUBLIC_FIREBASE_PROJECT_ID=your-project-id
NEXT_PUBLIC_FIREBASE_STORAGE_BUCKET=your-project.appspot.com
NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID=your-sender-id
NEXT_PUBLIC_FIREBASE_APP_ID=your-app-id

# Feature Flags
NEXT_PUBLIC_FIREBASE_PHONE_AUTH_ENABLED=true
NEXT_PUBLIC_FIREBASE_SIMULATION_MODE=false
```

### Simulation Mode

For development/testing without actual Firebase setup:

```env
NEXT_PUBLIC_FIREBASE_SIMULATION_MODE=true
```

When enabled:
- OTP is not actually sent
- Any 6-digit code is accepted
- Generates simulated Firebase ID tokens

## Backend Implementation

### Files Created

| File | Purpose |
|------|---------|
| `Options/FirebaseOptions.cs` | Firebase configuration options |
| `Auth/IFirebaseAuthService.cs` | Interface for Firebase token verification |
| `Auth/FirebaseAuthService.cs` | Firebase token verification implementation |
| `Auth/IFirebasePhoneAuthService.cs` | Interface for phone auth operations |
| `Auth/FirebasePhoneAuthService.cs` | Phone auth service implementation |
| `Controllers/FirebasePhoneAuthController.cs` | REST API endpoints |
| `Models/Auth/FirebasePhoneRegisterRequest.cs` | Register request model |
| `Models/Auth/UsernameLoginRequest.cs` | Login request model |
| `Models/Auth/FirebasePasswordResetRequest.cs` | Password reset request model |
| `Models/Auth/FirebasePhoneVerifyRequest.cs` | Verify phone request model |
| `Models/Auth/FirebasePhoneVerifyResponse.cs` | Verify phone response model |

### Configuration

Add to `appsettings.json`:

```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "ServiceAccountPath": "path/to/service-account.json",
    "SimulateVerification": false
  }
}
```

For development:

```json
{
  "Firebase": {
    "ProjectId": "your-firebase-project-id",
    "SimulateVerification": true
  }
}
```

### API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/auth/firebase-phone/verify` | Verify Firebase token & check if user exists |
| POST | `/api/auth/firebase-phone/register` | Register new user with username/password |
| POST | `/api/auth/firebase-phone/login` | Login with username/password |
| POST | `/api/auth/firebase-phone/reset-password` | Reset password after phone verification |

## User Flows

### Signup Flow

1. User enters phone number
2. Firebase sends OTP via SMS
3. User enters OTP code
4. Frontend verifies OTP with Firebase
5. Frontend gets Firebase ID token
6. Frontend sends token to backend `/verify` endpoint
7. Backend verifies token and confirms phone is not registered
8. User creates username and password
9. Frontend sends registration request to backend
10. Backend creates user and returns app auth token

### Login Flow

1. User enters username and password
2. Frontend sends credentials to `/login` endpoint
3. Backend validates credentials
4. Backend returns app auth token

### Forgot Password Flow

1. User enters phone number
2. Firebase sends OTP via SMS
3. User enters OTP code
4. Frontend verifies OTP with Firebase
5. Frontend gets Firebase ID token
6. Frontend sends token to backend `/verify` endpoint
7. Backend verifies token and confirms user exists
8. User enters new password
9. Frontend sends reset request to backend
10. Backend updates password and returns app auth token

## Security Considerations

1. **Firebase ID Token Verification**: Backend always verifies Firebase ID tokens before trusting phone numbers
2. **Password Hashing**: Passwords are hashed using ASP.NET Core Identity (bcrypt)
3. **Rate Limiting**: Firebase handles OTP rate limiting
4. **Account Lockout**: ASP.NET Core Identity handles failed login attempts
5. **Separation of Concerns**: OTP logic is frontend-only; backend only verifies tokens

## Production Setup

### Firebase Console Setup

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Create a new project or select existing
3. Enable Phone Authentication:
   - Go to Authentication > Sign-in method
   - Enable Phone provider
4. Get configuration:
   - Go to Project Settings > General
   - Under "Your apps", add a web app
   - Copy the configuration object
5. Download service account:
   - Go to Project Settings > Service accounts
   - Generate new private key
   - Save the JSON file securely

### Firebase Admin SDK (Optional)

For production, install the Firebase Admin SDK:

```bash
dotnet add package FirebaseAdmin
```

Then update `FirebaseAuthService.cs` to use actual token verification.

## Testing

### Frontend Testing

With simulation mode enabled, use any 6-digit code (e.g., `123456`) to verify OTP.

### Backend Testing

With `SimulateVerification: true`, send tokens in format:
```
simulated:{uid}:{phoneNumber}
```

Example:
```
simulated:user123:+919876543210
```

## Integration with Existing Auth

This implementation is completely independent from existing auth flows:
- ✅ Google login - Unchanged
- ✅ Gmail login - Unchanged  
- ✅ Azure AD login - Unchanged
- ✅ Existing email/password login - Unchanged

The Firebase Phone Auth uses:
- Separate API endpoints (`/api/auth/firebase-phone/*`)
- Separate UI pages (`/auth/phone-signup`, `/auth/phone-login`, `/auth/forgot-password`)
- Same user table with additional fields (PhoneNumber, Username)
- Same JWT token generation for app authentication
