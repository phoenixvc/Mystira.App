# Mystira.App

**A Dynamic Story-Driven Application for Child Development**

Mystira is an interactive storytelling platform featuring branching narratives with moral choice tracking, designed to support child development through engaging gameplay experiences. The application uses D&D-inspired scenarios with a moral compass system that tracks player choices and their impact.

## 🌟 Features

### Core Functionality
- **Interactive Story Scenarios**: Branching narratives with multiple paths and outcomes
- **Moral Compass System**: Track character development through decision-making
- **Echo System**: Record and analyze player choices and their moral implications
- **Game Session Management**: Real-time session tracking with choice history
- **Achievement System**: Reward player progress and milestones
- **Media-Rich Experience**: Support for images, audio, and multimedia content

### User Experience
- **Passwordless Authentication**: Secure, email-based magic code sign-up system
- **Progressive Web App (PWA)**: Installable web application with offline support
- **Age-Appropriate Content**: Content filtering and age group targeting (Preschool, School, Tween, Teen, Adult)
- **Character Customization**: Select and customize characters from character maps
- **Real-Time Game State**: Track session progress, pause, resume, and end games

### Administrative Tools
- **Scenario Management**: Create and manage branching story scenarios
- **Media Upload**: Azure Blob Storage integration for multimedia assets
- **User Management**: Account and profile management with COPPA compliance
- **Analytics**: Session statistics and player progress tracking
- **Health Monitoring**: Comprehensive health checks for production deployment

## 🏗️ Architecture

### Technology Stack

#### Backend
- **.NET 9.0**: Modern web API framework
- **ASP.NET Core Web API**: RESTful API with OpenAPI/Swagger
- **Azure Cosmos DB**: NoSQL database for structured data
- **Azure Blob Storage**: Multimedia asset storage
- **Azure Communication Services**: Email delivery for authentication
- **Entity Framework Core**: Data access layer with Cosmos DB provider

#### Frontend
- **Blazor WebAssembly**: .NET 8.0-based Progressive Web App
- **Service Workers**: Offline support and caching
- **IndexedDB**: Client-side data persistence
- **Markdig**: Markdown rendering for rich text content

#### Infrastructure
- **Azure App Service**: Cloud hosting platform
- **GitHub Actions**: CI/CD pipeline for automated deployment
- **Azure Static Web Apps**: PWA hosting and global CDN
- **Docker**: Containerization support

### Project Structure

```
Mystira.App/
├── src/
│   ├── Mystira.App.Api/              # Main backend API
│   ├── Mystira.App.Admin.Api/        # Administrative API
│   ├── Mystira.App.PWA/              # Blazor WebAssembly frontend
│   ├── Mystira.App.Domain/           # Domain models and business logic
│   └── Mystira.App.Infrastructure.Azure/  # Azure service integrations
├── tests/
│   ├── DMfinity.Api.Tests/           # API integration tests
│   ├── DMfinity.Domain.Tests/        # Domain model tests
│   └── DMfinity.Infrastructure.Azure.Tests/  # Infrastructure tests
├── Mystira.App.CosmosConsole/        # Database reporting tool
└── .github/workflows/                # CI/CD pipelines
```

## 🚀 Getting Started

### Prerequisites

- **.NET 8.0/9.0 SDK** - [Download](https://dotnet.microsoft.com/download)
- **Visual Studio 2022** or **VS Code** (optional)
- **Azure subscription** (for cloud deployment)
- **Git** for version control

### Local Development

#### 1. Clone the Repository
```bash
git clone https://github.com/phoenixvc/Mystira.App.git
cd Mystira.App
```

#### 2. Restore Dependencies
```bash
dotnet restore
```

#### 3. Configure Settings (Optional)

For local development, the API uses an in-memory database by default. For cloud features:

**API Configuration** (`src/Mystira.App.Api/appsettings.Development.json`):
```json
{
  "ConnectionStrings": {
    "CosmosDb": "your-cosmos-db-connection-string",
    "AzureStorage": "your-azure-storage-connection-string"
  },
  "AzureCommunicationServices": {
    "ConnectionString": "your-acs-connection-string",
    "SenderEmail": "DoNotReply@your-domain.azurecomm.net"
  }
}
```

#### 4. Run the Backend API
```bash
cd src/Mystira.App.Api
dotnet run
```

API will be available at:
- **HTTPS**: `https://localhost:5001`
- **HTTP**: `http://localhost:5000`
- **Swagger UI**: `https://localhost:5001/swagger`

#### 5. Run the PWA (Frontend)
In a separate terminal:
```bash
cd src/Mystira.App.PWA
dotnet run
```

PWA will be available at:
- **HTTPS**: `https://localhost:7000`
- **HTTP**: `http://localhost:5000`

### Building for Production

```bash
# Build entire solution
dotnet build --configuration Release

# Publish API
dotnet publish src/Mystira.App.Api -c Release -o ./publish/api

# Publish PWA
dotnet publish src/Mystira.App.PWA -c Release -o ./publish/pwa
```

### Docker Deployment

#### API Container
```bash
cd src/Mystira.App.Api
docker build -t mystira-app-api .
docker run -p 8080:80 mystira-app-api
```

## 📚 Documentation

### Quick Links
- **[Documentation Hub](docs/README.md)** - Complete documentation index
- **[Email Setup Guide](docs/setup/EMAIL_SETUP.md)** - Email integration with Azure Communication Services
- **[Passwordless Authentication](docs/features/PASSWORDLESS_SIGNUP.md)** - Technical implementation details
- **[Admin API Architecture](docs/features/ADMIN_API_SEPARATION.md)** - Admin/client API separation

### API Documentation
- **[Client API](src/Mystira.App.Api/README.md)** - Main client-facing API
- **[Admin API](src/Mystira.App.Admin.Api/README.md)** - Administrative API
- **[Cosmos Console](Mystira.App.CosmosConsole/README.md)** - Database reporting tool

### API Endpoints

#### Authentication
- `POST /api/auth/passwordless/signup` - Request passwordless signup code
- `POST /api/auth/passwordless/verify` - Verify code and create account

#### Scenarios
- `GET /api/scenarios` - List all scenarios with filtering
- `GET /api/scenarios/{id}` - Get specific scenario
- `POST /api/scenarios` - Create new scenario (Auth)
- `PUT /api/scenarios/{id}` - Update scenario (Auth)
- `DELETE /api/scenarios/{id}` - Delete scenario (Auth)

#### Game Sessions
- `POST /api/gamesessions` - Start new game session (Auth)
- `GET /api/gamesessions/{id}` - Get session details (Auth)
- `POST /api/gamesessions/choice` - Make choice in session (Auth)
- `POST /api/gamesessions/{id}/pause` - Pause session (Auth)
- `POST /api/gamesessions/{id}/resume` - Resume session (Auth)
- `POST /api/gamesessions/{id}/end` - End session (Auth)

#### User Profiles
- `POST /api/userprofiles` - Create user profile
- `GET /api/userprofiles/{name}` - Get profile (Auth)
- `PUT /api/userprofiles/{name}` - Update profile (Auth)

#### Media Management
- `POST /api/media/upload` - Upload media file (Auth)
- `GET /api/media/{blobName}/url` - Get media URL
- `GET /api/media/{blobName}/download` - Download media file

#### Health Checks
- `GET /api/health` - Comprehensive health check
- `GET /api/health/ready` - Readiness probe
- `GET /api/health/live` - Liveness probe

## 🔐 Authentication & Security

### Passwordless Sign-Up Flow
1. User enters email and display name
2. System generates 6-digit magic code
3. Code sent via Azure Communication Services email (or console in development)
4. User enters code to verify and create account
5. Account created with Auth0-compatible ID format

### Security Features
- **HTTPS Only** - All production endpoints require HTTPS
- **JWT Authentication** - Token-based authentication for DM accounts
- **COPPA Compliance** - No child accounts, DM-supervised access only
- **Input Validation** - Comprehensive validation on all API endpoints
- **Age-Appropriate Content** - Content filtering and validation
- **Data Encryption** - Transit and at-rest encryption for sensitive data

## 🎮 Core Domain Models

### Scenario
Defines an interactive story adventure with:
- Title, description, and tags
- Difficulty level and session length
- Character archetypes and age group targeting
- Scenes with branching choices
- Moral compass axes (up to 4)

### GameSession
Tracks active game state:
- Current scene and choice history
- Echo logs (moral choice tracking)
- Compass values and changes
- Session timing (start, pause, resume, end)
- Achievement tracking

### Account & UserProfile
User management:
- Auth0-compatible user IDs
- Display names and email addresses
- Profile preferences and settings
- Onboarding completion tracking

### PendingSignup
Temporary signup management:
- Email and display name
- 6-digit verification code
- 15-minute expiration
- One-time use enforcement

## 🧪 Testing

### Run Tests
```bash
# Run all tests
dotnet test

# Run specific test project
dotnet test tests/DMfinity.Api.Tests

# With coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Projects
- **DMfinity.Api.Tests**: API integration tests
- **DMfinity.Domain.Tests**: Domain model unit tests
- **DMfinity.Infrastructure.Azure.Tests**: Azure service integration tests

## 📦 Database Tools

### Cosmos Console
The `Mystira.App.CosmosConsole` project provides database reporting and management:

```bash
cd Mystira.App.CosmosConsole
dotnet run
```

Features:
- Account reporting and statistics
- Database health checks
- Data export capabilities

## 🌐 Deployment

### Azure Deployment

#### Automated CI/CD
GitHub Actions workflows automatically deploy on:
- **Push to `main`**: Production deployment
- **Push to `develop`**: Development environment
- **Pull requests**: Build and test validation
  - **Note**: Pull requests with titles containing `[WIP]` or `WIP:` will skip Azure Static Web Apps deployment to reduce staging environment usage

#### Manual Deployment
```bash
# Deploy API to Azure App Service
az webapp deployment source config-zip \
  --resource-group mystira-app-rg \
  --name mystira-app-api \
  --src ./publish/api.zip

# Deploy PWA to Azure Static Web Apps
swa deploy ./publish/pwa \
  --deployment-token $SWA_TOKEN
```

### Environment Variables

#### API Configuration
- `ASPNETCORE_ENVIRONMENT` - Environment (Development/Staging/Production)
- `ConnectionStrings__CosmosDb` - Cosmos DB connection string
- `ConnectionStrings__AzureStorage` - Azure Storage connection string
- `AzureCommunicationServices__ConnectionString` - ACS connection string
- `AzureCommunicationServices__SenderEmail` - Verified sender email

#### PWA Configuration
- `ApiBaseUrl` - Backend API URL (default: `https://mystira-app-dev-api.azurewebsites.net/`)

## 🤝 Contributing

We welcome contributions! Here's how to get started:

1. **Fork the repository**
2. **Create a feature branch**: `git checkout -b feature/your-feature-name`
3. **Make your changes** and commit: `git commit -am 'Add new feature'`
4. **Push to your fork**: `git push origin feature/your-feature-name`
5. **Create a Pull Request**

### Development Guidelines
- Follow existing code style and conventions
- Add unit tests for new features
- Update documentation as needed
- Ensure all tests pass before submitting PR
- Keep commits atomic and well-described

### Code Structure
- **Controllers**: API endpoints with input validation
- **Services**: Business logic and data access
- **Models**: Domain entities and DTOs
- **Infrastructure**: Cross-cutting concerns (logging, health checks)
- **Components**: Reusable Blazor UI components

## 📋 License

Copyright (c) 2025 Mystira Team. All rights reserved.

## 🙏 Acknowledgments

### Technologies
- [.NET](https://dotnet.microsoft.com/) - Application framework
- [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor) - Frontend framework
- [Azure](https://azure.microsoft.com/) - Cloud infrastructure
- [Cosmos DB](https://azure.microsoft.com/services/cosmos-db/) - NoSQL database
- [Azure Communication Services](https://azure.microsoft.com/services/communication-services/) - Email delivery

## 📞 Support

For questions, issues, or feature requests:
- **GitHub Issues**: [Create an issue](https://github.com/phoenixvc/Mystira.App/issues)
- **Email**: support@mystira.app
- **Documentation**: See docs in repository root

## 🗺️ Roadmap

### Current Features (✅ Completed)
- Passwordless authentication with email verification
- Interactive story scenarios with branching narratives
- Moral compass and echo tracking system
- Game session management
- Media asset management
- Progressive Web App with offline support

### Planned Features (🔄 In Progress)
- Real-time multiplayer sessions
- Voice narration support
- Enhanced character customization
- Parent/guardian dashboard
- Advanced analytics and reporting

### Future Enhancements (📋 Planned)
- Mobile native apps (iOS/Android)
- Social features and sharing
- Scenario marketplace
- AI-powered story generation
- Multi-language support

---

**Built with ❤️ by the Mystira Team**
