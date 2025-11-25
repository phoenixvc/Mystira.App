# ADR-0005: Separate API and Admin API

**Status**: ✅ Accepted

**Date**: 2025-11-24

**Deciders**: Development Team

**Tags**: architecture, api, security, separation-of-concerns

---

## Context

The Mystira.App application has two distinct types of operations:

1. **User Operations**: Actions performed by end-users on their own resources
   - Create game session for self
   - Update own user profile
   - Upload own media assets

2. **Admin Operations**: System-level actions affecting other users or system configuration
   - Delete any scenario
   - Modify badge configurations
   - Access any user's profile

Initially, these operations were mixed in a single API, which created several problems:

### Problems with Single API

1. **Security Concerns**
   - Admin endpoints exposed to public internet
   - Risk of unauthorized access to admin functions
   - Difficult to apply different security policies

2. **No Clear Separation**
   - User and admin operations mixed in same controllers
   - Hard to identify which operations are admin-only
   - Confusing for developers

3. **Authorization Complexity**
   - Single authorization strategy for all operations
   - Difficult to enforce admin-only access
   - Risk of authorization bugs

4. **Deployment Flexibility**
   - Cannot deploy admin API separately
   - Cannot restrict admin API to internal network
   - No option for different scaling strategies

### Considered Alternatives

1. **Single API with Authorization Attributes**
   - ✅ Simpler project structure
   - ✅ No code duplication
   - ❌ Admin endpoints exposed publicly
   - ❌ No deployment separation
   - ❌ Authorization errors could expose admin functions

2. **Single API with Different Base Routes** (`/api` vs `/admin`)
   - ✅ Simpler than separate projects
   - ✅ Clear URL distinction
   - ❌ Still deployed together
   - ❌ Cannot restrict network access
   - ❌ Same security boundary

3. **Separate API Projects** ⭐ **CHOSEN**
   - ✅ Complete physical separation
   - ✅ Can deploy independently
   - ✅ Different network access policies
   - ✅ Clear separation of concerns
   - ✅ Reduced attack surface for user API
   - ❌ Some code duplication in controllers
   - ❌ More complex project structure

---

## Decision

We will create two separate API projects:

1. **Mystira.App.Api** - User-facing API (public)
   - Route prefix: `/api/`
   - Operations on user's own resources
   - Public internet access
   - Standard user authentication

2. **Mystira.App.Admin.Api** - Admin API (internal)
   - Route prefix: `/adminapi/`
   - System-level and cross-user operations
   - Restricted network access (internal only)
   - Admin role required

### Routing Convention

**User API** (`/api/`):
```
POST   /api/gamesessions          - Create session for self
GET    /api/gamesessions/{id}     - Get own session
PUT    /api/userprofiles/me       - Update own profile
```

**Admin API** (`/adminapi/`):
```
DELETE /adminapi/scenarios/{id}   - Delete any scenario (admin only)
GET    /adminapi/users             - List all users (admin only)
PUT    /adminapi/badges/{id}       - Update badge config (admin only)
```

### Decision Criteria

An operation belongs in **Admin API** if it:
- Affects other users' resources
- Modifies system configuration
- Requires elevated permissions
- Is system-level (not user-specific)

An operation belongs in **User API** if it:
- Affects only the current user's resources
- Is self-service
- Requires only user authentication
- Is user-facing

---

## Consequences

### Positive Consequences ✅

1. **Security Isolation**
   - Admin endpoints not exposed to public internet
   - Reduced attack surface for user API
   - Can deploy Admin API on internal network only
   - Separate authentication/authorization policies

2. **Clear Separation of Concerns**
   - Obvious which operations are admin-only
   - Easier code review (identify admin operations)
   - Less confusion for new developers

3. **Deployment Flexibility**
   - Can deploy APIs separately
   - Admin API on internal network, User API on public
   - Different scaling strategies:
     - User API: High traffic, many instances
     - Admin API: Low traffic, single instance
   - Can update Admin API without affecting users

4. **Independent Versioning**
   - Can version Admin API separately
   - Breaking changes in Admin API don't affect users
   - Easier deprecation strategy

5. **Easier Auditing**
   - All admin operations logged in separate API
   - Clear audit trail for admin actions
   - Simpler compliance reporting

### Negative Consequences ❌

1. **Code Duplication**
   - Some controllers duplicated between APIs
   - Similar DTO mapping code
   - Mitigated by: Shared Application layer (Commands/Queries)

2. **More Complex Project Structure**
   - Two API projects instead of one
   - More Program.cs/Startup configurations
   - Mitigated by: Clear naming conventions, documentation

3. **Deployment Complexity**
   - Must deploy two APIs instead of one
   - More CI/CD pipelines
   - Mitigated by: Modern deployment tools handle multiple services easily

4. **Testing Overhead**
   - Must test both APIs
   - Integration tests for both
   - Mitigated by: Shared test utilities, same Application layer

---

## Implementation Details

### Project Structure

```
Mystira.App/
├── src/
│   ├── Mystira.App.Api/              # User-facing API (public)
│   │   ├── Controllers/
│   │   │   ├── GameSessionsController.cs    # User operations only
│   │   │   ├── UserProfilesController.cs    # Self-service
│   │   │   └── MediaController.cs           # Upload own media
│   │   └── Program.cs
│   │
│   ├── Mystira.App.Admin.Api/        # Admin API (internal)
│   │   ├── Controllers/
│   │   │   ├── ScenariosAdminController.cs  # System-level
│   │   │   ├── UsersAdminController.cs       # Access all users
│   │   │   └── BadgesAdminController.cs      # System config
│   │   └── Program.cs
│   │
│   └── Mystira.App.Application/      # Shared business logic
│       └── CQRS/                      # Used by both APIs
```

### Example Controller Comparison

**User API** - Self-Service:
```csharp
// Api/Controllers/UserProfilesController.cs
[ApiController]
[Route("api/userprofiles")]
[Authorize] // Standard user auth
public class UserProfilesController : ControllerBase
{
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
    {
        // User can only update their own profile
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var command = new UpdateUserProfileCommand(userId, request);
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
```

**Admin API** - System-Level:
```csharp
// Admin.Api/Controllers/UsersAdminController.cs
[ApiController]
[Route("adminapi/users")]
[Authorize(Roles = "Admin")] // Admin role required
public class UsersAdminController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllUsers()
    {
        // Admin can access all users
        var query = new GetAllUsersQuery();
        var users = await _mediator.Send(query);
        return Ok(users);
    }

    [HttpDelete("{userId}")]
    public async Task<IActionResult> DeleteUser(string userId)
    {
        // Admin can delete any user
        var command = new DeleteUserCommand(userId);
        await _mediator.Send(command);
        return NoContent();
    }
}
```

### Authorization Strategy

**User API**:
- `[Authorize]` - Standard user authentication
- User can only access own resources
- Validate user ID from JWT claims

**Admin API**:
- `[Authorize(Roles = "Admin")]` - Admin role required
- Admin can access any resources
- Additional audit logging for all operations

### Deployment Configuration

**User API**:
- Deploy to public-facing load balancer
- High availability (multiple instances)
- CDN for static assets
- Rate limiting for public access

**Admin API**:
- Deploy to internal network (VPN required)
- Single instance sufficient
- No public internet access
- Stricter logging and monitoring

---

## Related Decisions

- **ADR-0003**: Adopt Hexagonal Architecture (both APIs are input adapters)
- **ADR-0001**: Adopt CQRS Pattern (both APIs share same Commands/Queries)

---

## References

- [API Design Patterns](https://www.martinfowler.com/articles/richardsonMaturityModel.html)
- [Security by Design](https://owasp.org/www-project-top-ten/)
- [ARCHITECTURAL_RULES.md](../ARCHITECTURAL_RULES.md) - API routing rules

---

## Notes

- Both APIs share the same Application layer (Commands, Queries, Use Cases)
- Only controllers are duplicated, business logic is shared
- Admin API must always have admin role authorization
- User API validates user can only access own resources

---

## License

Copyright (c) 2025 Mystira. All rights reserved.
