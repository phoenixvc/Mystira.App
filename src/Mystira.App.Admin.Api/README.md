# Mystira.App Admin API

Administrative API for content management and system administration.

## Overview

The Mystira.App Admin API provides backend services for administrative and content management operations, separate from the client-facing API. This separation ensures better security, scalability, and maintainability.

**Key Features:**
- **Content Management**: Create, update, and delete scenarios, media, and characters
- **Admin Dashboard**: Web-based UI for system administration
- **Media Upload**: Bulk upload and management of multimedia assets
- **System Configuration**: Badge configurations, character maps, and app status
- **Data Import/Export**: YAML-based import/export for content management

## Architecture

- **.NET 9.0 Web API** with Azure App Service deployment
- **Shared Azure Cosmos DB** with client API for data persistence
- **Azure Blob Storage** for multimedia assets
- **JWT Authentication** for admin access
- **RESTful API Design** with OpenAPI/Swagger documentation
- **MVC Views** for admin dashboard UI
- **Port 7001** for local development (client API uses 7000)

## Separation from Client API

This Admin API is separate from the client-facing API (`Mystira.App.Api`) to:
- **Enhance Security**: Admin operations isolated from public endpoints
- **Independent Scaling**: Admin and client can scale separately
- **Better Maintainability**: Clear separation of concerns
- **Flexible Deployment**: Can deploy to different infrastructure

See [Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md) for architectural details.

## Prerequisites

- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Visual Studio 2022 or VS Code

## Getting Started

### Local Development

1. **Navigate to Admin API directory**
   ```bash
   cd src/Mystira.App.Admin.Api
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Configure settings**
   Update `appsettings.Development.json` with your Azure connection strings:
   ```json
   {
     "ConnectionStrings": {
       "CosmosDb": "your-cosmos-db-connection-string",
       "AzureStorage": "your-azure-storage-connection-string"
     },
     "Jwt": {
       "Key": "your-jwt-secret-key"
     }
   }
   ```
   
   **Note**: Use the same connection strings as the client API since they share the database.

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Admin Dashboard**
   Navigate to `https://localhost:7001/admin` or `http://localhost:7001/admin`

### Docker Deployment

1. **Build Docker image**
   ```bash
   docker build -t mystira-app-api .
   ```

2. **Run container**
   ```bash
   docker run -p 8080:80 -e ConnectionStrings__CosmosDb="your-connection-string" mystira-app-api
   ```

## API Endpoints

### Admin Dashboard (UI)
- `GET /admin` - Main dashboard
- `GET /admin/login` - Login page
- `GET /admin/scenarios` - Scenario management page
- `GET /admin/media` - Media management page
- `GET /admin/media-metadata` - Media metadata management
- `GET /admin/charactermaps` - Character maps management
- `GET /admin/status` - App status management
- `POST /admin/status` - Update app status

### Scenario Management (API)
- `POST /api/admin/scenariosadmin` - Create scenario
- `PUT /api/admin/scenariosadmin/{id}` - Update scenario
- `DELETE /api/admin/scenariosadmin/{id}` - Delete scenario
- `POST /api/admin/scenariosadmin/validate` - Validate scenario
- `GET /api/admin/scenariosadmin/{id}` - Get scenario details

### Media Management (API)
- `POST /api/admin/mediaadmin/upload` - Upload single media file
- `POST /api/admin/mediaadmin/bulk-upload` - Bulk upload media
- `PUT /api/admin/mediaadmin/{mediaId}` - Update media asset
- `DELETE /api/admin/mediaadmin/{mediaId}` - Delete media asset
- `POST /api/admin/mediaadmin/validate` - Validate media references
- `GET /api/admin/mediaadmin/statistics` - Get usage statistics

### Character Management (API)
- `POST /api/admin/characteradmin` - Add character
- `PUT /api/admin/characteradmin/{id}` - Update character
- `DELETE /api/admin/characteradmin/{id}` - Delete character

### Character Map Management (API)
- `POST /api/admin/charactermapsadmin` - Create character map
- `PUT /api/admin/charactermapsadmin/{id}` - Update character map
- `DELETE /api/admin/charactermapsadmin/{id}` - Delete character map
- `POST /api/admin/charactermapsadmin/import` - Import from YAML
- `GET /api/admin/charactermapsadmin/export` - Export to YAML

### Badge Configuration (API)
- `POST /api/badgeconfigurationsadmin` - Create badge configuration
- `PUT /api/badgeconfigurationsadmin/{id}` - Update badge configuration
- `DELETE /api/badgeconfigurationsadmin/{id}` - Delete badge configuration
- `POST /api/badgeconfigurationsadmin/import` - Import from YAML
- `GET /api/badgeconfigurationsadmin/export` - Export to YAML

## Authentication

The Admin API uses JWT Bearer token authentication for admin access. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

Admin authentication is required for all content management operations.

## Running with Client API

For full functionality, run both APIs simultaneously:

```bash
# Terminal 1 - Client API (port 7000)
cd src/Mystira.App.Api
dotnet run

# Terminal 2 - Admin API (port 7001)
cd src/Mystira.App.Admin.Api
dotnet run
```

Both APIs connect to the same database, so changes in the Admin API are immediately available in the Client API.

## Data Models

### Admin-Managed Entities

- **Scenario** - Interactive story definitions with scenes and choices
- **MediaAsset** - Multimedia files stored in Azure Blob Storage
- **Character** - Character definitions for scenarios
- **CharacterMap** - Mappings of characters to media assets
- **BadgeConfiguration** - Achievement badge definitions

All models are shared with the Client API through the Domain project.

## Azure Deployment

### Infrastructure

The Admin API should be deployed separately from the Client API:

- **Client API**: `mystiraapp.azurewebsites.net`
- **Admin API**: `admin-mystiraapp.azurewebsites.net` or subdomain

Both connect to the same Cosmos DB database and Azure Blob Storage.

### CI/CD Pipeline

Configure GitHub Actions workflow to deploy Admin API to its own App Service:

```yaml
- name: Deploy Admin API
  uses: azure/webapps-deploy@v2
  with:
    app-name: 'mystira-admin-api'
    package: './admin-api-package'
```

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Staging, Production)
- `ConnectionStrings__CosmosDb` - Cosmos DB connection string (shared with Client API)
- `ConnectionStrings__AzureStorage` - Azure Storage connection string (shared with Client API)
- `Jwt__Key` - JWT signing key (must match Client API)
- `Jwt__Issuer` - JWT issuer claim
- `Jwt__Audience` - JWT audience claim

**Important**: Use the same database and storage credentials as the Client API.

## CORS Configuration

The Admin API has a separate CORS policy (`MystiraAdminPolicy`) that allows:
- `http://localhost:7001`
- `https://localhost:7001`
- `https://admin.mystiraapp.azurewebsites.net`
- `https://admin.mystira.app`

## Security

- **HTTPS Only** - All endpoints require HTTPS in production
- **JWT Authentication** - Admin-only access with token validation
- **CORS Configuration** - Restricted to admin domains only
- **Input Validation** - Comprehensive validation on all endpoints
- **Isolated from Client API** - Enhanced security through separation
- **Data Encryption** - All data encrypted in transit and at rest

## Development Guidelines

### Code Structure
- **Controllers** - Admin API endpoints and dashboard views
- **Services** - Business logic for content management
- **Models** - Request/response DTOs and view models
- **Views** - Razor views for admin dashboard UI

### Admin Dashboard Features
- Scenario creation and editing
- Media upload and management
- Character map configuration
- Badge configuration
- System status monitoring

## Related Documentation

- **[Admin API Separation](../../docs/features/ADMIN_API_SEPARATION.md)** - Architecture details
- **[Client API README](../Mystira.App.Api/README.md)** - Client API documentation
- **[Main README](../../README.md)** - Project overview

## License

Copyright (c) 2025 Mystira. All rights reserved.
