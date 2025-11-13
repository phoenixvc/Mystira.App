# Mystira.App API

Backend API infrastructure for Mystira.App - Dynamic Story App for Child Development

## Overview

The Mystira.App API provides the core backend services for the Mystira.App MAUI mobile application, implementing:

- **Scenario Management**: CRUD operations for D&D scenarios with branching narratives
- **Game Session Management**: Real-time session tracking with choice history and moral compass
- **User Profile Management**: DM account management with COPPA compliance
- **Media Asset Management**: Azure Blob Storage integration for multimedia content
- **Echo & Compass Tracking**: Moral choice tracking and achievement system
- **Health Monitoring**: Comprehensive health checks for production deployment

## Architecture

- **.NET 9.0 Web API** with Azure App Service deployment
- **Azure Cosmos DB** for structured data persistence
- **Azure Blob Storage** for multimedia assets
- **JWT Authentication** for DM-only access (no child accounts per COPPA)
- **RESTful API Design** with OpenAPI/Swagger documentation
- **Health Checks** for monitoring and diagnostics

## Prerequisites

- .NET 9.0 SDK
- Azure subscription (for cloud deployment)
- Visual Studio 2022 or VS Code

## Getting Started

### Local Development

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd Mystira.App.Api
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

4. **Run the application**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**
   Navigate to `https://localhost:5001` or `http://localhost:5000`

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

### Scenarios
- `GET /api/scenarios` - Get scenarios with filtering
- `GET /api/scenarios/{id}` - Get specific scenario
- `POST /api/scenarios` - Create new scenario (Auth required)
- `PUT /api/scenarios/{id}` - Update scenario (Auth required)
- `DELETE /api/scenarios/{id}` - Delete scenario (Auth required)
- `GET /api/scenarios/age-group/{ageGroup}` - Get age-appropriate scenarios
- `GET /api/scenarios/featured` - Get featured scenarios

### Game Sessions
- `POST /api/gamesessions` - Start new session (Auth required)
- `GET /api/gamesessions/{id}` - Get session details (Auth required)
- `GET /api/gamesessions/dm/{dmName}` - Get DM's sessions (Auth required)
- `POST /api/gamesessions/choice` - Make choice in session (Auth required)
- `POST /api/gamesessions/{id}/pause` - Pause session (Auth required)
- `POST /api/gamesessions/{id}/resume` - Resume session (Auth required)
- `POST /api/gamesessions/{id}/end` - End session (Auth required)
- `GET /api/gamesessions/{id}/stats` - Get session statistics (Auth required)
- `DELETE /api/gamesessions/{id}` - Delete session (Auth required)

### User Profiles
- `POST /api/userprofiles` - Create DM profile
- `GET /api/userprofiles/{name}` - Get profile (Auth required)
- `PUT /api/userprofiles/{name}` - Update profile (Auth required)
- `DELETE /api/userprofiles/{name}` - Delete profile (Auth required)
- `POST /api/userprofiles/{name}/complete-onboarding` - Complete onboarding (Auth required)

### Media
- `POST /api/media/upload` - Upload media file (Auth required)
- `GET /api/media/{blobName}/url` - Get media URL
- `GET /api/media/{blobName}/download` - Download media file
- `GET /api/media` - List media files (Auth required)
- `DELETE /api/media/{blobName}` - Delete media file (Auth required)

### Health
- `GET /api/health` - Comprehensive health check
- `GET /api/health/ready` - Readiness probe
- `GET /api/health/live` - Liveness probe

### Version
- `GET /api/version` - Get API version information (version number, API name, build date, environment)

## Authentication

The API uses JWT Bearer token authentication for DM accounts. Include the token in the Authorization header:

```
Authorization: Bearer <your-jwt-token>
```

## Data Models

### Scenario
Core adventure definition with scenes, choices, and branching narratives.

### GameSession
Session lifecycle tracking with choice history, echo logging, and compass values.

### UserProfile
DM account information with fantasy theme preferences and age group settings.

### EchoLog
Moral choice tracking with strength values and echo types from master list.

### CompassTracking
Real-time moral axis tracking with historical changes and threshold monitoring.

## Azure Deployment

### Infrastructure as Code

Use the provided Bicep templates for Azure deployment:

```bash
# Deploy all infrastructure
az deployment group create \
  --resource-group mystira-app-prod-rg \
  --template-file Infrastructure/Azure/main.bicep \
  --parameters environment=prod
```

### CI/CD Pipeline

GitHub Actions workflow is configured for:
- **Build & Test** on every push
- **Deploy to Development** on develop branch
- **Deploy to Production** on main branch
- **Infrastructure Deployment** with commit message `[deploy-infra]`

## Configuration

### Environment Variables

- `ASPNETCORE_ENVIRONMENT` - Environment name (Development, Staging, Production)
- `ConnectionStrings__CosmosDb` - Cosmos DB connection string
- `ConnectionStrings__AzureStorage` - Azure Storage connection string
- `Jwt__Key` - JWT signing key
- `Jwt__Issuer` - JWT issuer claim
- `Jwt__Audience` - JWT audience claim

## Security

- **HTTPS Only** - All endpoints require HTTPS in production
- **JWT Authentication** - DM accounts only, no child accounts (COPPA compliance)
- **CORS Configuration** - Restricted to known frontend domains
- **Input Validation** - Comprehensive validation on all endpoints
- **Content Filtering** - Age-appropriate content validation
- **Data Encryption** - All personal data encrypted in transit and at rest

## Monitoring

- **Health Checks** - Database and Blob Storage connectivity
- **Application Insights** - Performance and error monitoring
- **Structured Logging** - JSON formatted logs for Azure monitoring

## Development Guidelines

### Code Structure
- **Controllers** - API endpoints with input validation
- **Services** - Business logic and data access
- **Models** - Request/response DTOs
- **Infrastructure** - Cross-cutting concerns (health checks, etc.)

### Testing
- Unit tests for all services
- Integration tests for API endpoints
- Health check validation

### Content Safety
- All scenarios validated against age appropriateness
- Echo/compass values within specified ranges
- Maximum limits enforced (4 archetypes, 4 compass axes per scenario)

## License

Copyright (c) 2025 Mystira Team. All rights reserved.