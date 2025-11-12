# Mystira App - Admin API Separation

## Overview
This document describes the separation of admin/content management functionality from the main client API into a dedicated Admin API project.

## Architectural Changes

### New Project Structure
- **Mystira.App.Api** (Main/Client API) - Port 7000
  - Client-specific read operations
  - User-facing functionality
  - Game session management
  - User profiles and badges

- **Mystira.App.Admin.Api** (New Admin API) - Port 7001
  - Content creation and management
  - Admin-only endpoints
  - Scenario, Media, Character, Badge configuration management
  - Admin dashboard and UI

## Moved Functionality to Admin API

### Controllers
- **AdminController** - Main admin dashboard and UI endpoints
- **ScenariosAdminController** - Create, update, delete scenarios
- **MediaAdminController** - Upload, update, delete media assets
- **CharacterAdminController** - Create, update, delete characters
- **CharacterMapsAdminController** - Create, update, delete character maps
- **BadgeConfigurationsController** - Create, update, delete badge configurations

### Services
All admin-specific services are registered in the Admin API's Program.cs:
- ScenarioApiService
- CharacterMapApiService
- CharacterMapFileService
- MediaApiService
- MediaMetadataService
- CharacterMediaMetadataService
- BundleService
- BadgeConfigurationApiService
- AppStatusService

## Remaining Client API Functionality

### Controllers
- **ScenariosController** - GET: List, Get by ID, Get featured, Get by age group
- **MediaController** - GET: List, Get by ID, Get by filename, Download
- **CharacterController** - GET: Get by ID
- **CharacterMapsController** - GET: List all, Get by ID
- **BadgeConfigurationsController** - GET: List all, Get by ID, Get by axis
- **GameSessionsController** - Full game session management
- **UserBadgesController** - User badge operations
- **UserProfilesController** - User profile operations
- **AccountsController** - Account management
- **AuthController** - Authentication (signup, signin, passwordless)

## API Routes

### Main Client API (Mystira.App.Api)
```
GET  /api/scenarios
GET  /api/scenarios/{id}
GET  /api/scenarios/age-group/{ageGroup}
GET  /api/scenarios/featured

GET  /api/media
GET  /api/media/{mediaId}
GET  /api/media/{mediaId}/info
GET  /api/media/by-filename/{fileName}
GET  /api/media/url/{fileName}
GET  /api/media/file/{fileName}

GET  /api/characters/{id}

GET  /api/charactermaps
GET  /api/charactermaps/{id}

GET  /api/badgeconfigurations
GET  /api/badgeconfigurations/{id}
GET  /api/badgeconfigurations/axis/{axis}

POST /api/gamesessions
GET  /api/gamesessions/{id}
... (game session routes)

... (auth, user profiles, badges routes)
```

### Admin API (Mystira.App.Admin.Api)
```
# Admin Dashboard
GET /admin
GET /admin/login
GET /admin/scenarios
GET /admin/media
GET /admin/media-metadata
GET /admin/character-media-metadata
GET /admin/charactermaps
GET /admin/status
POST /admin/status

# Scenario Management
POST   /api/admin/scenariosadmin
PUT    /api/admin/scenariosadmin/{id}
DELETE /api/admin/scenariosadmin/{id}
POST   /api/admin/scenariosadmin/validate
GET    /api/admin/scenariosadmin/{id}

# Media Management
POST   /api/admin/mediaadmin/upload
POST   /api/admin/mediaadmin/bulk-upload
PUT    /api/admin/mediaadmin/{mediaId}
DELETE /api/admin/mediaadmin/{mediaId}
POST   /api/admin/mediaadmin/validate

# Character Management
POST   /api/admin/characteradmin
PUT    /api/admin/characteradmin/{id}
DELETE /api/admin/characteradmin/{id}

# Character Map Management
POST   /api/admin/charactermapsadmin
PUT    /api/admin/charactermapsadmin/{id}
DELETE /api/admin/charactermapsadmin/{id}
POST   /api/admin/charactermapsadmin/import
GET    /api/admin/charactermapsadmin/export

# Badge Configuration Management
POST   /api/badgeconfigurationsadmin
PUT    /api/badgeconfigurationsadmin/{id}
DELETE /api/badgeconfigurationsadmin/{id}
POST   /api/badgeconfigurationsadmin/import
GET    /api/badgeconfigurationsadmin/export
```

## Database
- Both APIs share the same Cosmos DB database (or in-memory for development)
- Single database connection string used by both projects
- No data duplication or separation

## Authentication
- Both APIs use the same JWT/Cookie authentication scheme
- Shared authentication configuration
- Admin API requires admin authentication for protected endpoints
- Client API requires user authentication for game session operations

## Configuration
Both projects have identical configuration files:
- `appsettings.json`
- `appsettings.Development.json`
- `appsettings.Testing.json`

Key configuration keys are shared:
- `ConnectionStrings:CosmosDb`
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- Azure Blob Storage configuration
- Azure Communication Services configuration

## CORS Configuration

### Client API (MystiraAppPolicy)
Allows origins:
- http://localhost:7000
- https://localhost:7000
- https://mystiraapp.azurewebsites.net
- https://mystira.app

### Admin API (MystiraAdminPolicy)
Allows origins:
- http://localhost:7001
- https://localhost:7001
- https://admin.mystiraapp.azurewebsites.net
- https://admin.mystira.app

## Deployment

### Development
Run both services locally:
```bash
# Terminal 1 - Client API
cd src/Mystira.App.Api
dotnet run

# Terminal 2 - Admin API
cd src/Mystira.App.Admin.Api
dotnet run
```

### Production
- Deploy Mystira.App.Api to main application (e.g., mystiraapp.azurewebsites.net)
- Deploy Mystira.App.Admin.Api to admin subdomain (e.g., admin.mystiraapp.azurewebsites.net)
- Both services connect to the same Cosmos DB database

## Benefits
1. **Separation of Concerns** - Admin and client functionality clearly separated
2. **Independent Scaling** - Admin and client APIs can scale independently
3. **Security** - Admin endpoints isolated from client API
4. **Maintainability** - Clear boundaries between admin and client code
5. **Flexibility** - Can deploy to different servers/regions as needed

## Migration Path
No breaking changes to the client API. All existing endpoints remain functional.
Clients continue to use existing endpoints. Admin functionality redirects to Admin API.

## Future Considerations
- Implement API gateway for unified access
- Add authentication/authorization service
- Implement distributed caching between services
- Add service-to-service communication for validations
