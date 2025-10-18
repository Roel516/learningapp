# Testing JWT Authentication with Swagger

This guide shows you how to test the API's JWT authentication using Swagger UI in development mode.

## Accessing Swagger UI

1. **Start the application** in development mode:
   ```bash
   cd C:\case\src\LearningResourcesApp
   dotnet run
   ```

2. **Open Swagger UI** in your browser:
   ```
   https://localhost:5001/swagger
   ```
   (Replace `xxx` with the actual port shown in console)

3. You should see the **Learning Resources API** documentation with all available endpoints.

## Step-by-Step Testing Guide

### Step 1: Test Login (Get JWT Token)

1. **Locate the `/api/Account/login` endpoint** in the Swagger UI
2. Click **"Try it out"**
3. Enter the admin credentials in the request body:

   **For JWT-only testing (recommended for Swagger):**
   ```json
   {
     "email": "admin@admin.nl",
     "wachtwoord": "admin123",
     "useCookieAuth": false
   }
   ```

   **For testing with cookies (Blazor behavior):**
   ```json
   {
     "email": "admin@admin.nl",
     "wachtwoord": "admin123",
     "useCookieAuth": true
   }
   ```

   **Note:** Set `useCookieAuth: false` to test pure JWT authentication. This prevents cookie creation and ensures your JWT token is the only authentication method.
4. Click **"Execute"**
5. In the response, you should see:
   ```json
   {
     "succes": true,
     "foutmelding": null,
     "gebruiker": {
       "id": "...",
       "naam": "Administrator",
       "email": "admin@admin.nl",
       "isInterneMedewerker": true
     },
     "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI..."
   }
   ```
6. **Copy the token value** (the long string starting with `eyJ...`)

### Step 2: Authorize Swagger with the Token

1. **Look for the "Authorize" button** at the top right of the Swagger UI page (it has a lock icon 🔒)
2. Click the **"Authorize"** button
3. In the dialog that appears, you'll see:
   - A text field labeled "Value"
   - Placeholder text: `Bearer {token}`
4. **Paste your token** in the format:
   ```
   Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiI...
   ```
   ⚠️ **Important**: Include the word "Bearer " (with a space) before the token!
5. Click **"Authorize"**
6. You should see the lock icon change to indicate you're authenticated 🔓
7. Click **"Close"**

### Step 3: Test Protected Endpoints

Now you can test any protected endpoint with your token automatically included!

#### Example 1: Get All Learning Resources

1. Find the **GET `/api/Leermiddelen`** endpoint
2. Click **"Try it out"**
3. Click **"Execute"**
4. You should receive a **200 OK** response with the list of resources

#### Example 2: Create a New Resource

1. Find the **POST `/api/Leermiddelen`** endpoint
2. Click **"Try it out"**
3. Enter the resource details:
   ```json
   {
     "titel": "Testing with Swagger",
     "beschrijving": "A guide to API testing",
     "link": "https://swagger.io/docs/"
   }
   ```
4. Click **"Execute"**
5. You should receive a **201 Created** response

#### Example 3: Get All Users (Admin Only)

1. Find the **GET `/api/Account/users`** endpoint
2. Click **"Try it out"**
3. Click **"Execute"**
4. Since you're authenticated as admin with `InterneMedewerker = true`, you should receive:
   - **200 OK** with the list of users
5. If you try this with a non-admin token, you'll get:
   - **403 Forbidden**

### Step 4: Test Without Authentication

To test what happens when authentication fails:

1. Click the **"Authorize"** button again
2. Click **"Logout"** to clear the token
3. Try accessing a protected endpoint
4. You should receive:
   - **401 Unauthorized**

## Testing Different User Types

### Testing as a Regular User (Non-Admin)

1. **Register a new user**:
   - POST `/api/Account/register`
   ```json
   {
     "naam": "Test User",
     "email": "test@example.com",
     "wachtwoord": "password123",
     "isSelfRegistration": false
   }
   ```
2. **Copy the token** from the response
3. **Authorize with the new token**
4. Try accessing `/api/Account/users`:
   - You should get **403 Forbidden** (not an admin)
5. Try accessing `/api/Leermiddelen`:
   - You should get **200 OK** (regular users can view resources)

### Testing as Admin

Use the pre-seeded admin account:
- Email: `admin@admin.nl`
- Password: `admin123`
- Has `InterneMedewerker` claim = can access all endpoints

## Common Swagger Testing Scenarios

### Scenario 1: Full CRUD Test for Learning Resources

```
1. Login as admin → Get token → Authorize
2. GET /api/Leermiddelen → View all resources
3. POST /api/Leermiddelen → Create new resource
4. GET /api/Leermiddelen/{id} → View specific resource
5. PUT /api/Leermiddelen/{id} → Update resource
6. DELETE /api/Leermiddelen/{id} → Delete resource
```

### Scenario 2: User Management Test

```
1. Login as admin → Get token → Authorize
2. GET /api/Account/users → View all users
3. POST /api/Account/register → Create new user
4. PUT /api/Account/users/{userId}/toggle-internal-employee → Promote user to admin
5. GET /api/Account/users → Verify user is now admin
```

### Scenario 3: Pure JWT Authentication Test

```
1. POST /api/Account/register → Register new user
2. POST /api/Account/login → Login with new credentials (returns JWT token)
3. Authorize in Swagger with the JWT token
4. GET /api/Account/users → Test with JWT (should work with token)
5. POST /api/Account/logout → Logout (clears cookies only, not JWT)
6. GET /api/Account/users → Test again with same JWT token (should STILL work!)
```

**Important**: JWT tokens are stateless and cannot be invalidated server-side. The `/api/Account/logout` endpoint only clears cookies, so your JWT token will continue to work until it expires (7 days).

## Visual Guide

### Before Authorization
```
┌─────────────────────────────────────┐
│  🔒 Authorize                       │
│                                     │
│  GET /api/Leermiddelen              │
│  ❌ Lock icon = Not authenticated   │
└─────────────────────────────────────┘
```

### After Authorization
```
┌─────────────────────────────────────┐
│  🔓 Authorize                       │
│                                     │
│  GET /api/Leermiddelen              │
│  ✅ Open lock = Authenticated       │
└─────────────────────────────────────┘
```

## Response Codes Reference

| Code | Meaning | When You'll See It |
|------|---------|-------------------|
| 200 OK | Success | Successful GET, PUT requests |
| 201 Created | Resource created | Successful POST requests |
| 204 No Content | Success, no body | Successful DELETE requests |
| 400 Bad Request | Invalid input | Missing required fields, validation errors |
| 401 Unauthorized | Not authenticated | No token or invalid token |
| 403 Forbidden | No permission | Valid token but insufficient permissions |
| 404 Not Found | Resource not found | Invalid ID or deleted resource |

## Troubleshooting

### Problem: "401 Unauthorized" after authorizing

**Causes:**
- Forgot to include "Bearer " before the token
- Token has expired (7 days expiration)
- Copy/paste error (token got truncated)

**Solution:**
1. Click "Authorize" again
2. Make sure format is: `Bearer eyJ...`
3. Get a fresh token by logging in again

### Problem: "403 Forbidden" on admin endpoints

**Causes:**
- User doesn't have `InterneMedewerker` claim
- Using regular user token instead of admin token

**Solution:**
1. Logout from Swagger
2. Login as admin (`admin@admin.nl` / `admin123`)
3. Authorize with the admin token

### Problem: Token not working after logging out

**This is actually the expected behavior!**

**What's happening:**
- Login creates a JWT token (stateless, 7-day expiration)
- Login does NOT create a cookie session anymore (fixed in latest version)
- Logout only clears cookie sessions (doesn't affect JWT tokens)
- Your JWT token should continue working even after logout

**If token stops working after logout:**
This was a bug in the original implementation where login was creating both a cookie AND a JWT token. The fix was to use `UserManager.CheckPasswordAsync` instead of `SignInManager.PasswordSignInAsync` to avoid creating cookie sessions.

**Solution:**
- Your JWT token should work even after calling logout
- If it doesn't, make sure you're using the latest version of AccountController.cs

### Problem: Token not working after app restart

**Cause:**
- Token signature depends on the JWT secret key
- Secret key may have changed (if using development secrets)

**Solution:**
- Get a new token by logging in again after restart

### Problem: Can't see "Authorize" button

**Cause:**
- Swagger not properly configured for JWT
- Running in production mode instead of development

**Solution:**
1. Ensure `ASPNETCORE_ENVIRONMENT=Development`
2. Verify Swagger configuration in Program.cs
3. Restart the application

## Advanced: Testing with Multiple Tabs

You can test different user sessions simultaneously:

1. **Tab 1**: Authorize as admin
   - Test admin-only endpoints
   - View all users

2. **Tab 2**: Authorize as regular user
   - Test regular user access
   - Verify 403 on admin endpoints

3. **Tab 3**: No authorization
   - Test public endpoints
   - Verify 401 on protected endpoints

## Swagger Configuration Details

The Swagger UI is configured in `Program.cs` with JWT support:

```csharp
builder.Services.AddSwaggerGen(options =>
{
    // API documentation
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Learning Resources API",
        Version = "v1",
        Description = "API voor beheer van leermiddelen met JWT authenticatie ondersteuning"
    });

    // JWT authentication support
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Voorbeeld: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    // Require authentication for all endpoints
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
```

## Comparing Cookie vs JWT Authentication in Swagger

| Feature | Cookie Auth | JWT Auth |
|---------|-------------|----------|
| How to use | Login with `useCookieAuth: true` | Login with `useCookieAuth: false` |
| Visible in Swagger | No (handled by browser) | Yes (manual Authorization header) |
| Best for testing | Blazor frontend behavior | External API consumers |
| Persistence | Session-based | Token-based (7 days) |
| Testing logout | Works (cookie cleared) | Doesn't work (token still valid) |
| When to use | Testing Blazor app | Testing external APIs |

### The `useCookieAuth` Flag

The login endpoint now supports a `useCookieAuth` parameter to control authentication behavior:

- **`useCookieAuth: true`** (default)
  - Creates a cookie session + returns JWT token
  - Best for: Blazor WebAssembly client (uses cookies for subsequent requests)
  - Behavior: Browser automatically sends cookies on each request
  - Logout: Clears cookie session

- **`useCookieAuth: false`**
  - Returns JWT token only, no cookie created
  - Best for: External API consumers (mobile apps, desktop apps, other services)
  - Behavior: Client must manually send JWT token in Authorization header
  - Logout: Does nothing (JWT tokens can't be invalidated server-side)

### Why Two Authentication Methods?

1. **Cookie Auth** (Blazor app)
   - Automatic: Browser handles cookie storage and sending
   - Secure: HttpOnly cookies can't be accessed by JavaScript
   - Session-based: Can be invalidated server-side

2. **JWT Auth** (External consumers)
   - Flexible: Works across different platforms and devices
   - Stateless: No server-side session storage needed
   - Portable: Easy to copy, test, and share (securely)

For Swagger testing, **JWT (with `useCookieAuth: false`) is more convenient** because you can:
- Easily switch between different users
- Copy/paste tokens between tools
- See exactly what external consumers experience
- Test token expiration scenarios
- Test authentication independently from browser cookies

## Quick Reference: Test Credentials

| User | Email | Password | Type |
|------|-------|----------|------|
| Admin | admin@admin.nl | admin123 | InterneMedewerker |
| Custom | (register your own) | - | Regular User |

## Next Steps

After testing in Swagger:
1. Review the [JWT Authentication Guide](./JWT-Authentication.md) for implementation details
2. Test the same endpoints with external tools (Postman, cURL, etc.)
3. Integrate JWT authentication in your client applications

## Tips for Efficient Testing

1. **Keep a test token handy**: Copy your admin token to a text file for quick access
2. **Use the "Schemas" section**: View request/response models at the bottom of Swagger UI
3. **Check the "Responses" section**: See example responses before executing
4. **Use "Try it out" liberally**: Swagger won't modify your database unless you execute POST/PUT/DELETE
5. **Monitor the console**: Watch for authentication logs and errors in the terminal
