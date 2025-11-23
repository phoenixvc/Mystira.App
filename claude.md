# Claude.md - AI Assistant Guidance for Mystira Application Suite

## Project Overview

**Mystira** is a dynamic storytelling and character development platform for children, featuring:
- Interactive narrative experiences with branching storylines
- Character progression and developmental tracking
- Offline-first PWA with Blazor WebAssembly
- Parent oversight and COPPA compliance design (in progress)
- Azure-hosted backend (Cosmos DB, Blob Storage)

**Key Business Context:**
- **Target Users:** Children (primary), Parents/Guardians (secondary), Content Creators (tertiary)
- **Regulatory:** COPPA compliance required (partially implemented - CRITICAL GAP)
- **SLA Targets:** 99.95% uptime, P99 latency < 2 seconds
- **Scale Target:** 10,000 concurrent users

## Architecture

### **Hexagonal (Ports & Adapters) Architecture**

```
┌─────────────────────────────────────────────────────────┐
│  API Layer (Controllers only - NO business logic)      │
│    /api/*       - Public endpoints (user's own data)   │
│    /adminapi/*  - Admin endpoints (system/other users) │
└──────────────────────┬──────────────────────────────────┘
                       │ DTOs (Contracts)
┌──────────────────────▼──────────────────────────────────┐
│  Application Layer (Use Cases, Orchestration)           │
│    - Use Cases (one per business action)                │
│    - Ports (repository interfaces)                      │
│    - Application Services (orchestration only)          │
└──────────────────────┬──────────────────────────────────┘
                       │ Domain Models
┌──────────────────────▼──────────────────────────────────┐
│  Domain Layer (Pure business logic, no dependencies)    │
│    - Entities, Value Objects, Domain Events             │
│    - Business invariants and rules                      │
└──────────────────────────────────────────────────────────┘
                       ▲
┌──────────────────────┴──────────────────────────────────┐
│  Infrastructure Layer (Adapters - EF, Azure, Discord)   │
│    - Repository implementations                          │
│    - External API adapters                               │
│    - Azure services (Cosmos DB, Blob Storage, Email)    │
│    - Discord bot integration                             │
└─────────────────────────────────────────────────────────┘
```

### **Critical Architectural Rules**

**From `docs/architecture/ARCHITECTURAL_RULES.md`:**

1. **API Layer:** Controllers ONLY. No business logic, no services.
   - Maps DTOs → Use Case input models
   - Handles routing, validation, auth attributes
   - NEVER call repositories directly

2. **Application Layer:** THE ONLY location for:
   - Use Cases (one class per business action)
   - Application Services (orchestration)
   - Ports (repository interfaces)

3. **Domain Layer:** Pure business logic
   - No infrastructure dependencies
   - No DTOs
   - No framework dependencies (currently targets `netstandard2.1` - should be `net9.0`)

4. **Infrastructure Layer:** Adapters only
   - Repository implementations
   - External API clients
   - No business logic

5. **API vs AdminAPI Routing Rule:**
   - `/api/*` → User acting on their own resources
   - `/adminapi/*` → System-level or other users' data

**ARCHITECTURAL VIOLATION ALERT:** `src/Mystira.App.Api/Services/*` violates these rules. Services should be in Application layer. See **PERF-4** in production review.

## Technology Stack

### **Core Technologies**
- **.NET 9.0** (target framework)
  - ⚠️ **WARNING:** `global.json` still references SDK 8.0.415 - needs update to 9.0.x
- **C# 12** (latest language features)
- **ASP.NET Core 9.0** (Web APIs)
- **Entity Framework Core 8.0.16** (should upgrade to 9.0)
- **Blazor WebAssembly 9.0** (PWA frontend)

### **Frontend**
- **Blazor WebAssembly** with offline support
- **Service Worker** for caching
- **IndexedDB** for client-side persistence
- **CSS Custom Properties** for theming
- **Scoped CSS** for component styles

### **Backend**
- **Azure Cosmos DB** (NoSQL via EF Core provider)
- **Azure Blob Storage** (media assets)
- **Azure Communication Services** (email)
- **JWT Authentication** (RS256 asymmetric + HS256 symmetric fallback)

### **Integrations**
- **Discord.Net 3.16.0** (bot integration)
- **Story Protocol** (⚠️ stub implementation only)

### **Tooling**
- **Husky.Net** (pre-commit hooks - `dotnet format`)
- **GitHub Actions** (CI/CD)
- **Azure Bicep** (infrastructure as code)

## Critical Security & Compliance Issues

### **🔴 CRITICAL - Immediate Action Required**

1. **Production Secrets Exposed** (BUG-1)
   - **Location:** `src/Mystira.App.Api/appsettings.json:12-14`
   - **Issue:** Cosmos DB and Storage account keys hardcoded in version control
   - **Action:** NEVER commit secrets. Use Azure Key Vault or User Secrets
   - **Status:** Requires immediate key rotation + remediation

2. **COPPA Compliance NOT Implemented** (FEAT-INC-1)
   - **Location:** Documented but not implemented
   - **Issue:** Operating children's platform without parental consent, age verification, or data controls
   - **Risk:** $50,000+ FTC fines per violation, legal shutdown
   - **Action:** Consult legal counsel, implement age gate, parental consent system, parent dashboard

3. **PII Logged Without Redaction** (BUG-4)
   - **Location:** `AuthController.cs` and throughout
   - **Issue:** Email addresses (PII) logged in plaintext
   - **Action:** Implement PII masking/redaction for all logs

### **🟡 HIGH Priority**

4. **No Rate Limiting** (BUG-5)
   - Auth endpoints lack brute-force protection
   - Action: Implement ASP.NET Core rate limiting middleware

5. **Test Coverage ~4.3%** (PERF-6)
   - Only 18 test files for 414 source files
   - Action: Target 60%+ coverage, prioritize critical paths

6. **Blazor Optimizations Disabled** (PERF-1, PERF-2)
   - AOT compilation and IL linking disabled
   - Impact: 2-3x larger bundles, slow load times
   - Action: Enable for production builds

## Development Guidelines

### **When Adding/Modifying Features**

1. **Follow Hexagonal Architecture:**
   ```
   Controller → Use Case → Domain Entity → Repository
   ```

2. **Never Skip Layers:**
   - ❌ Controller → Repository
   - ✅ Controller → Use Case → Repository

3. **One Use Case per Business Action:**
   - `CreateGameSessionUseCase`
   - `UpdateUserProfileUseCase`
   - `DeleteScenarioUseCase`

4. **DTOs in Contracts Project Only:**
   - Request DTOs: `Mystira.App.Contracts/Requests/`
   - Response DTOs: `Mystira.App.Contracts/Responses/`

### **Code Quality Standards**

From `docs/best-practices.md`:

1. **Security:**
   - Validate ALL input at controller level
   - Use `[Authorize]` for sensitive endpoints
   - NEVER hardcode secrets
   - Use `System.Security.Cryptography.RandomNumberGenerator` for crypto operations
   - Strict CORS whitelist (NO wildcards)

2. **Performance:**
   - Always use async/await (`...Async()` methods)
   - Avoid N+1 queries (use `.Include()` or projection)
   - Lazy load non-critical Blazor components
   - Use `@key` for list rendering

3. **Testing:**
   - Unit tests for domain logic (required)
   - Integration tests for API endpoints
   - Target high coverage for critical paths

4. **Accessibility:**
   - WCAG 2.1 AA compliance required
   - Semantic HTML
   - ARIA labels for interactive elements
   - Keyboard navigation support
   - Sufficient color contrast

5. **CSS Styling:**
   - Use Blazor **Scoped CSS** (`.razor.css` files) for component-specific styles
   - Global CSS (`app.css`) for design system foundations only
   - NO CSS Modules (designed for JavaScript, not Blazor)
   - See `docs/features/CSS_STYLING_APPROACH.md`

### **Commit Standards**

- Follow [Conventional Commits](https://www.conventionalcommits.org/):
  ```
  feat: Add guardian dashboard feature
  fix: Correct CORS policy vulnerability
  docs: Update API documentation
  test: Add unit tests for GameSessionUseCase
  refactor: Extract authentication configuration
  ```

- Pre-commit hooks automatically run `dotnet format` via Husky.Net

## Common Workflows

### **Adding a New API Endpoint**

1. **Create Request/Response DTOs** in `Mystira.App.Contracts/`
2. **Create Use Case** in `Mystira.App.Application/UseCases/`
3. **Register Use Case** in `Program.cs` DI container
4. **Create Controller Method** in appropriate API project:
   - `/api` for user's own data
   - `/adminapi` for system/admin operations
5. **Add Tests** in corresponding test project
6. **Document** in Swagger with examples

### **Adding a New Blazor Component**

1. **Create Component** in `src/Mystira.App.PWA/Components/` or `Pages/`
2. **Create Scoped CSS** (`.razor.css` alongside `.razor`)
3. **Use Design Tokens** from `app.css`:
   ```css
   color: var(--primary-color);
   background: var(--card);
   ```
4. **Ensure Accessibility:**
   - Semantic HTML
   - ARIA labels
   - Keyboard navigation
5. **Test Offline Behavior** (service worker caching)

### **Modifying Database Schema**

1. **Update Domain Model** in `Mystira.App.Domain/Models/`
2. **Update Repository Interface** in `Application/` (port)
3. **Update Repository Implementation** in `Infrastructure.Data/Repositories/`
4. **Update Use Cases** as needed
5. **Migration:** Cosmos DB is schema-less, but coordinate breaking changes
6. **Test** thoroughly (integration tests)

### **Deploying Changes**

1. **PR Checklist** (from `CONTRIBUTING.md`):
   - [ ] Code formatted (`dotnet format` or pre-commit hook)
   - [ ] Tests added/updated
   - [ ] Documentation updated
   - [ ] No secrets committed
   - [ ] Architectural rules followed
   - [ ] COPPA implications considered

2. **CI/CD:**
   - GitHub Actions automatically build and test
   - Deployment workflows in `.github/workflows/`:
     - `mystira-app-api-cicd-*.yml` (API deployments)
     - `azure-static-web-apps-*.yml` (PWA deployments)
     - `infrastructure-deploy-dev.yml` (Bicep templates)

3. **Environments:**
   - **Dev:** Continuous deployment from `main` branch
   - **Prod:** Requires manual approval or tag

## Project Structure Reference

```
Mystira.App/
├── src/
│   ├── Mystira.App.Domain/              # Core business models (netstandard2.1 ⚠️)
│   ├── Mystira.App.Application/         # Use Cases, Ports
│   ├── Mystira.App.Contracts/           # DTOs (Requests/Responses)
│   ├── Mystira.App.Infrastructure.Data/ # EF Core, Repositories
│   ├── Mystira.App.Infrastructure.Azure/# Azure services (Blob, Email)
│   ├── Mystira.App.Infrastructure.Discord/ # Discord bot
│   ├── Mystira.App.Infrastructure.StoryProtocol/ # Story Protocol (stub)
│   ├── Mystira.App.Api/                 # Public API
│   ├── Mystira.App.Admin.Api/           # Admin API
│   └── Mystira.App.PWA/                 # Blazor WebAssembly
├── tests/
│   ├── Mystira.App.Api.Tests/
│   ├── Mystira.App.Admin.Api.Tests/
│   └── Mystira.App.Infrastructure.Discord.Tests/
├── docs/
│   ├── architecture/                    # Architectural rules & patterns
│   ├── domain/models/                   # Domain model documentation
│   ├── features/                        # Feature documentation
│   ├── setup/                           # Setup guides
│   ├── usecases/                        # Use case documentation
│   └── best-practices.md                # Development standards
├── infrastructure/                      # Azure Bicep templates
├── tools/
│   └── Mystira.App.CosmosConsole/       # Operational CLI tool
└── examples/
    └── DiscordBotExample/               # Example integrations
```

## Key Documentation

### **Must-Read Docs**

1. **Architecture:**
   - `docs/architecture/ARCHITECTURAL_RULES.md` (CRITICAL)
   - `docs/architecture/patterns/HEXAGONAL_ARCHITECTURE.md`
   - `docs/architecture/patterns/REPOSITORY_PATTERN.md`

2. **Development:**
   - `docs/best-practices.md`
   - `CONTRIBUTING.md`
   - `docs/features/CSS_STYLING_APPROACH.md`

3. **Domain:**
   - `docs/domain/models/README.md`
   - Individual model docs in `docs/domain/models/`

4. **Use Cases:**
   - `docs/usecases/README.md`
   - Specific use case docs (e.g., `docs/usecases/gamesessions/create-game-session.md`)

5. **Roadmap:**
   - `docs/POTENTIAL_ENHANCEMENTS_ROADMAP.md` (comprehensive 1100+ line roadmap)
   - `PRODUCTION_REVIEW_REPORT.md` (this review's findings)

### **API Documentation**

- **Swagger UI:** Available at root (`/`) when API is running
- **OpenAPI Spec:** `/swagger/v1/swagger.json`

## Known Issues & TODOs

### **From Code Analysis**

The codebase contains ~14 TODO comments indicating incomplete work:

1. **Badge thresholds hardcoded** (should use BadgeConfigurationApiService)
2. **Scenario validation relaxed** (master axis list not finalized)
3. **Media management status check** not implemented
4. **Story Protocol** entire service is stub (`TODO: Implement actual SDK integration`)
5. **Character assignment persistence** not implemented

### **From Production Review**

See `PRODUCTION_REVIEW_REPORT.md` for comprehensive list of 40+ identified issues, prioritized by severity.

**Top 5 Critical Items:**
1. BUG-1: Production secrets exposed (immediate action)
2. FEAT-INC-1: COPPA compliance not implemented (legal blocker)
3. BUG-4: PII logged without redaction (compliance violation)
4. PERF-6: Test coverage ~4.3% (reliability risk)
5. BUG-5: No rate limiting (security vulnerability)

## Testing Guidance

### **Current State**

- **Coverage:** ~4.3% (18 test files / 414 source files) - **CRITICALLY LOW**
- **Framework:** xUnit (inferred from test project structure)
- **CI Integration:** Tests run in GitHub Actions

### **Testing Strategy (from TASK-2)**

Target test pyramid:
- **70% Unit Tests** (domain logic, use cases)
- **20% Integration Tests** (API endpoints, repositories)
- **10% E2E Tests** (critical user flows)

**Priority Areas:**
1. Authentication flows (security-critical)
2. Game session management (core business logic)
3. COPPA/parental consent (compliance-critical)
4. Repository implementations
5. Use cases

**Coverage Targets:**
- **Minimum:** 60% overall
- **Critical paths:** 80%+
- **Domain layer:** 90%+

### **Writing Tests**

Example structure:
```csharp
public class CreateGameSessionUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ValidInput_CreatesSession()
    {
        // Arrange
        var mockRepo = new Mock<IGameSessionRepository>();
        var mockUnitOfWork = new Mock<IUnitOfWork>();
        var useCase = new CreateGameSessionUseCase(mockRepo.Object, mockUnitOfWork.Object);

        // Act
        var result = await useCase.ExecuteAsync(new CreateGameSessionInput { ... });

        // Assert
        Assert.NotNull(result);
        mockRepo.Verify(r => r.AddAsync(It.IsAny<GameSession>()), Times.Once);
    }
}
```

## Configuration

### **Environment Variables / App Settings**

**Required Configuration:**

1. **Azure Cosmos DB:**
   ```json
   "ConnectionStrings": {
     "CosmosDb": "AccountEndpoint=...;AccountKey=...;"
   }
   ```
   ⚠️ **NEVER commit connection strings to version control**

2. **Azure Storage:**
   ```json
   "ConnectionStrings": {
     "AzureStorage": "DefaultEndpointsProtocol=https;AccountName=...;AccountKey=...;"
   }
   ```

3. **JWT Settings:**
   ```json
   "JwtSettings": {
     "Issuer": "your-issuer",
     "Audience": "your-audience",
     "RsaPublicKey": "PEM-encoded-public-key",
     // OR
     "JwksEndpoint": "https://your-auth-provider/.well-known/jwks.json",
     // LEGACY (avoid in production):
     "SecretKey": "symmetric-key"
   }
   ```

4. **CORS:**
   ```json
   "CorsSettings": {
     "AllowedOrigins": "https://mystira.app,https://www.mystira.app"
   }
   ```

5. **Azure Communication Services (Email):**
   ```json
   "AzureCommunicationServices": {
     "ConnectionString": "endpoint=...;accesskey=...",
     "SenderEmail": "DoNotReply@mystira.azurecomm.net"
   }
   ```

6. **Discord (optional):**
   ```json
   "Discord": {
     "Enabled": true,
     "BotToken": "your-bot-token"
   }
   ```

### **Configuration Best Practices**

1. **Development:** Use User Secrets (`dotnet user-secrets set`)
2. **Production:** Use Azure Key Vault
3. **Testing:** Use In-Memory Database (automatic fallback if CosmosDb connection string missing)

## Performance Optimization

### **Blazor Bundle Size**

**Current Issues:**
- AOT compilation disabled → slower runtime, larger bundles
- IL Linking disabled → 30-50% larger bundles

**Actions (from PERF-1, PERF-2):**

In `Mystira.App.PWA.csproj`:
```xml
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <RunAOTCompilation>true</RunAOTCompilation>
  <BlazorWebAssemblyEnableLinking>true</BlazorWebAssemblyEnableLinking>
</PropertyGroup>
```

### **Database Queries**

- Always use async methods: `.ToListAsync()`, `.FirstOrDefaultAsync()`
- Avoid N+1 queries: use `.Include()` or projections
- Example:
  ```csharp
  // ❌ Bad - N+1 query
  var sessions = await _dbSet.ToListAsync();
  foreach (var session in sessions)
  {
      var scenario = await _scenarioRepo.GetByIdAsync(session.ScenarioId);
  }

  // ✅ Good - single query with include
  var sessions = await _dbSet
      .Include(s => s.Scenario)
      .ToListAsync();
  ```

### **Caching Strategy**

- **Service Worker:** Caches static assets (configured in `service-worker.js`)
- **IndexedDB:** Client-side data persistence (mentioned in README, implementation details in PWA)
- **CDN:** Azure Static Web Apps provides CDN (needs optimization - see PERF-5)

## Troubleshooting

### **Common Issues**

1. **Build Fails with SDK Version Error**
   - **Cause:** `global.json` references SDK 8.0.415 but projects target .NET 9
   - **Fix:** Update `global.json` to SDK 9.0.100 or higher (BUG-2)

2. **Service Worker Not Updating**
   - **Cause:** Aggressive caching
   - **Fix:** `clearCacheAndReload()` function in `index.html`, or manually clear browser cache

3. **Cosmos DB Connection Fails**
   - **Cause:** Missing or invalid connection string
   - **Fallback:** App automatically uses In-Memory database for local development
   - **Fix:** Verify `ConnectionStrings:CosmosDb` in configuration

4. **JWT Authentication Fails**
   - **Check:** `JwtSettings` configuration
   - **Verify:** Public key format (PEM) or JWKS endpoint reachability
   - **Logs:** Check Application Insights or console logs

5. **CORS Errors**
   - **Verify:** `CorsSettings:AllowedOrigins` includes your frontend URL
   - **Check:** No trailing slashes in origin URLs
   - **Middleware Order:** `app.UseCors()` must be between `UseRouting()` and `UseAuthentication()`

### **Debugging**

1. **API Debugging:**
   - Swagger UI at `/` for testing endpoints
   - Enable detailed logging in `appsettings.Development.json`:
     ```json
     "Logging": {
       "LogLevel": {
         "Default": "Debug",
         "Microsoft.EntityFrameworkCore": "Information"
       }
     }
     ```

2. **Blazor Debugging:**
   - Browser DevTools → Application → Service Workers
   - Browser DevTools → Application → IndexedDB
   - Check console for Blazor errors

3. **Health Checks:**
   - **API Health:** `GET /health`
   - Includes Blob Storage health check
   - Discord bot health check (if enabled)

## AI Assistant Specific Guidance

### **When Making Changes**

1. **Always Reference Architecture:**
   - Check if proposed change violates hexagonal architecture
   - Ensure services go in Application layer, not API layer
   - Verify dependency direction

2. **Security First:**
   - NEVER commit secrets
   - Always validate input
   - Consider COPPA implications for children's data
   - Add rate limiting for new auth endpoints

3. **Test Coverage:**
   - Add tests for ALL new use cases
   - Add integration tests for new API endpoints
   - Maintain or improve overall coverage percentage

4. **Documentation:**
   - Update relevant docs in `/docs/`
   - Add XML comments for public APIs
   - Update Swagger descriptions
   - Consider PRD implications

### **When Reviewing Code**

Use this checklist from `docs/architecture/ARCHITECTURAL_RULES.md`:

- [ ] No business logic in controllers
- [ ] No services in API/AdminAPI layers
- [ ] DTOs only in Contracts project
- [ ] Use cases in Application layer
- [ ] Domain entities contain business invariants
- [ ] Infrastructure contains only adapters
- [ ] Correct routing (`/api` vs `/adminapi`)
- [ ] Proper dependency direction
- [ ] Tests added for new functionality
- [ ] No secrets committed
- [ ] COPPA implications considered

### **When Uncertain**

1. **Check Documentation:** Review `/docs/architecture/`, `/docs/best-practices.md`
2. **Follow Patterns:** Look at existing implementations (e.g., existing use cases, controllers)
3. **Ask User:** If business requirements or COPPA implications unclear
4. **Reference Review:** Check `PRODUCTION_REVIEW_REPORT.md` for known issues to avoid

## Key Contacts & Resources

- **Repository:** https://github.com/phoenixvc/Mystira.App (inferred)
- **Main Branch:** `main`
- **Feature Branches:** `claude/*` pattern for AI assistant work
- **Issue Tracker:** GitHub Issues
- **Documentation:** `/docs/` directory

## Version Information

- **.NET SDK:** Should be 9.0.100+ (currently `global.json` shows 8.0.415 - needs fix)
- **.NET Target Framework:** net9.0
- **EF Core:** 8.0.16 (should upgrade to 9.0)
- **Blazor WebAssembly:** 9.0.0
- **Discord.Net:** 3.16.0

## Final Notes

- **Production Readiness:** Project has strong foundations but CRITICAL gaps (secrets exposure, COPPA compliance, test coverage)
- **Priority:** Address BUG-1 (secrets), FEAT-INC-1 (COPPA), PERF-6 (testing) before production launch
- **Architecture:** Well-documented and mostly adhered to; some violations exist (services in API layer)
- **Documentation:** Comprehensive - use it! 72 markdown files covering architecture, domain, features, use cases
- **Roadmap:** Detailed enhancement roadmap exists in `POTENTIAL_ENHANCEMENTS_ROADMAP.md`

**Remember:** This is a children's platform. Privacy, safety, and compliance are not optional. When in doubt about COPPA implications, consult documentation or ask for clarification.

---

*Last Updated: November 23, 2025*
*Review Report: PRODUCTION_REVIEW_REPORT.md*
