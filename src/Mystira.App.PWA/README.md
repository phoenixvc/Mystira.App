# Mystira.App.PWA

Progressive Web Application (PWA) built with Blazor WebAssembly, serving as the primary user interface for the Mystira platform. This project is a **primary adapter** in the hexagonal architecture, translating user interactions into application use cases.

## Role in Hexagonal Architecture

**Layer**: **Presentation - UI Adapter (Primary/Driving)**

The PWA is a **primary adapter** (driving adapter) that:
- **Drives** the application by initiating use cases
- **Presents** domain data to users via interactive UI
- **Translates** user actions into API calls
- **Adapts** HTTP/REST to application needs
- **Manages** client-side state and routing

**Dependency Flow**:
```
User Interactions
    ↓ triggers
PWA UI Components (THIS)
    ↓ calls
REST API (Mystira.App.Api)
    ↓ calls
Application Layer (Use Cases)
    ↓ uses
Domain Layer (Core)
```

**Key Principles**:
- ✅ **Primary Adapter** - Drives the application (initiates actions)
- ✅ **Technology Specific** - Uses Blazor WebAssembly framework
- ✅ **Thin Presentation** - UI logic only, no business rules
- ✅ **API Communication** - Calls backend API via HTTP clients
- ✅ **Offline-First** - PWA capabilities with service worker

## Project Structure

```
Mystira.App.PWA/
├── Pages/
│   ├── Home.razor                      # Landing page
│   ├── About.razor                     # About page
│   ├── SignUp.razor                    # User registration
│   ├── SignIn.razor                    # Authentication
│   ├── ProfilesPage.razor              # User profile management
│   ├── GameSessionPage.razor           # Active game session UI
│   ├── CharacterAssignmentPage.razor   # Character selection
│   └── SimpleTest.razor                # Testing/demo page
├── Services/
│   ├── IApiClient.cs                   # Base API client interface
│   ├── BaseApiClient.cs                # Base HTTP client logic
│   ├── IScenarioApiClient.cs           # Scenario API interface
│   ├── ScenarioApiClient.cs            # Scenario API implementation
│   ├── IGameSessionApiClient.cs        # Game session API interface
│   ├── GameSessionApiClient.cs         # Game session API implementation
│   ├── IDiscordApiClient.cs            # Discord API interface
│   ├── DiscordApiClient.cs             # Discord integration
│   ├── AuthApiClient.cs                # Authentication API
│   ├── CharacterApiClient.cs           # Character API
│   ├── MediaApiClient.cs               # Media asset API
│   ├── AuthHeaderHandler.cs            # JWT token injection
│   ├── LocalStorageTokenProvider.cs    # Token storage
│   └── IndexedDbService.cs             # IndexedDB for offline data
├── Components/
│   └── (Shared Blazor components)
├── wwwroot/
│   ├── appsettings.json                # App configuration
│   ├── service-worker.js               # PWA service worker
│   ├── manifest.json                   # PWA manifest
│   ├── css/                            # Stylesheets
│   ├── images/                         # Static images
│   └── sounds/                         # Audio assets
├── Program.cs                          # App startup
└── Mystira.App.PWA.csproj
```

## Core Concepts

### Blazor WebAssembly

Runs .NET code directly in the browser via WebAssembly:
- **Client-side rendering**: No server-side rendering required
- **Single Page Application (SPA)**: Fast navigation
- **Offline capable**: Works without internet connection
- **.NET in browser**: Share code with backend

### Progressive Web App (PWA)

Installable web app with native-like experience:
- **Service Worker**: Offline caching and background sync
- **App Manifest**: Install on home screen
- **Push Notifications**: (Future feature)
- **Responsive Design**: Works on all devices

## Pages and Components

### Home.razor
Landing page with:
- Hero section introducing Mystira
- Feature highlights
- Call-to-action buttons
- Navigation to sign up/sign in

### SignUp.razor
User registration:
- Account creation form
- Email validation
- Password strength requirements
- COPPA compliance notices

### SignIn.razor
Authentication:
- Email/password login
- JWT token management
- Remember me functionality
- Redirect after login

### ProfilesPage.razor
User profile management:
- Display name and avatar
- Age group preference
- Fantasy theme selection
- Onboarding status
- Badge collection display

### GameSessionPage.razor
Interactive story gameplay:
- **Scene Display**: Current scene narrative
- **Choice Buttons**: Player decision options
- **Compass Display**: Real-time moral compass visualization
- **Echo Reveals**: Moral feedback after choices
- **Character Portraits**: Visual character representation
- **Media Playback**: Audio/video for scenes
- **Session Controls**: Pause, resume, save

### CharacterAssignmentPage.razor
Character selection:
- Available characters for scenario
- Archetype descriptions
- Character portraits
- Assign characters to players

## API Client Services

### Base Architecture

All API clients inherit from `BaseApiClient`:

```csharp
public abstract class BaseApiClient
{
    protected readonly HttpClient _httpClient;
    protected readonly ILogger _logger;

    protected async Task<T?> GetAsync<T>(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }

    protected async Task<T?> PostAsync<T>(string endpoint, object data)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, data);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>();
    }
}
```

### ScenarioApiClient

```csharp
public interface IScenarioApiClient
{
    Task<IEnumerable<Scenario>> GetScenariosAsync();
    Task<Scenario?> GetScenarioAsync(string id);
    Task<IEnumerable<Scenario>> GetByAgeGroupAsync(AgeGroup ageGroup);
    Task<IEnumerable<Scenario>> GetFeaturedAsync();
}
```

**Usage**:
```csharp
@inject IScenarioApiClient ScenarioClient

var scenarios = await ScenarioClient.GetByAgeGroupAsync(AgeGroup.Ages7to9);
```

### GameSessionApiClient

```csharp
public interface IGameSessionApiClient
{
    Task<GameSession> StartSessionAsync(StartSessionRequest request);
    Task<GameSession?> GetSessionAsync(string sessionId);
    Task<ChoiceResult> MakeChoiceAsync(string sessionId, MakeChoiceRequest request);
    Task PauseSessionAsync(string sessionId);
    Task ResumeSessionAsync(string sessionId);
    Task EndSessionAsync(string sessionId);
}
```

**Usage**:
```csharp
@inject IGameSessionApiClient SessionClient

var session = await SessionClient.StartSessionAsync(new StartSessionRequest
{
    ScenarioId = scenarioId,
    UserId = userId
});
```

### AuthApiClient

Manages authentication:
- `RegisterAsync(RegisterRequest)`: Create new account
- `LoginAsync(LoginRequest)`: Authenticate user
- `LogoutAsync()`: Clear session
- `RefreshTokenAsync()`: Refresh JWT token

### MediaApiClient

Media asset management:
- `GetMediaUrlAsync(string blobName)`: Get media URL
- `UploadMediaAsync(Stream, string)`: Upload media file
- `DownloadMediaAsync(string)`: Download media

### DiscordApiClient

Discord integration:
- `SendNotificationAsync(...)`: Send Discord messages
- `CreateSessionThreadAsync(...)`: Create game session thread
- `UpdateSessionStatusAsync(...)`: Update Discord status

## Authentication Flow

### JWT Token Management

1. **Login**: User submits credentials
2. **Token Receipt**: API returns JWT token
3. **Token Storage**: Stored in browser `localStorage`
4. **Token Injection**: Added to API requests via `AuthHeaderHandler`
5. **Token Refresh**: Auto-refresh before expiration

### AuthHeaderHandler

Automatically adds JWT to HTTP requests:

```csharp
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly ILocalStorageTokenProvider _tokenProvider;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _tokenProvider.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
```

### LocalStorageTokenProvider

Manages token persistence:

```csharp
public interface ILocalStorageTokenProvider
{
    Task<string?> GetTokenAsync();
    Task SetTokenAsync(string token);
    Task ClearTokenAsync();
}
```

## Offline Capabilities

### Service Worker

`service-worker.js` provides offline functionality:
- **Cache API responses**: Scenarios, sessions, media
- **Background sync**: Sync choices when online
- **Offline fallback**: Show cached content
- **Update strategy**: Cache-first with network fallback

### IndexedDB

Client-side database for offline data:

```csharp
public class IndexedDbService
{
    public async Task SaveScenarioAsync(Scenario scenario);
    public async Task<Scenario?> GetScenarioAsync(string id);
    public async Task SaveSessionAsync(GameSession session);
    public async Task SyncPendingChangesAsync();
}
```

**Use Cases**:
- Cache scenarios for offline play
- Store session progress locally
- Queue choices for sync when online

## State Management

### Component State

Each Blazor component manages local state:

```razor
@code {
    private GameSession? _currentSession;
    private bool _isLoading = true;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _isLoading = true;
            _currentSession = await SessionClient.GetSessionAsync(SessionId);
        }
        catch (Exception ex)
        {
            _errorMessage = ex.Message;
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Shared State (Future)

Consider state management libraries:
- **Fluxor**: Redux-like state management
- **Blazor State**: Simple state container
- **MediatR**: Message-based communication

## Configuration

### appsettings.json

```json
{
  "ApiBaseUrl": "https://api.mystira.app",
  "Environment": "Production",
  "Features": {
    "EnableOfflineMode": true,
    "EnableDiscordIntegration": true,
    "EnablePushNotifications": false
  },
  "Cache": {
    "ScenarioCacheDuration": "01:00:00",
    "MediaCacheDuration": "24:00:00"
  }
}
```

### Environment-Specific Config

- `appsettings.Development.json`: Local API (http://localhost:7000)
- `appsettings.Production.json`: Production API (https://api.mystira.app)

## Dependency Injection

Register services in `Program.cs`:

```csharp
var builder = WebAssemblyHostBuilder.CreateDefault(args);

// HTTP Client
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// API Clients
builder.Services.AddScoped<IScenarioApiClient, ScenarioApiClient>();
builder.Services.AddScoped<IGameSessionApiClient, GameSessionApiClient>();
builder.Services.AddScoped<IAuthApiClient, AuthApiClient>();
builder.Services.AddScoped<IMediaApiClient, MediaApiClient>();
builder.Services.AddScoped<IDiscordApiClient, DiscordApiClient>();

// Auth
builder.Services.AddTransient<AuthHeaderHandler>();
builder.Services.AddScoped<ILocalStorageTokenProvider, LocalStorageTokenProvider>();

// Offline
builder.Services.AddScoped<IndexedDbService>();

await builder.Build().RunAsync();
```

## Responsive Design

PWA uses responsive CSS for all screen sizes:
- **Mobile**: Optimized touch targets, simplified navigation
- **Tablet**: Enhanced layout, side panels
- **Desktop**: Full-featured UI, multi-column layouts

### Media Queries

```css
/* Mobile */
@media (max-width: 767px) {
    .game-session { flex-direction: column; }
}

/* Tablet */
@media (min-width: 768px) and (max-width: 1023px) {
    .sidebar { width: 250px; }
}

/* Desktop */
@media (min-width: 1024px) {
    .main-content { max-width: 1200px; }
}
```

## Accessibility

WCAG 2.1 AA compliance:
- **Semantic HTML**: Proper heading hierarchy
- **ARIA Labels**: Screen reader support
- **Keyboard Navigation**: All actions keyboard-accessible
- **Color Contrast**: Minimum 4.5:1 ratio
- **Focus Indicators**: Visible focus states

## Performance Optimization

### Lazy Loading

Load pages on-demand:
```csharp
@page "/game-session/{SessionId}"
@attribute [Lazy]
```

### Code Splitting

Blazor automatically splits code by page.

### Image Optimization

- Use WebP format
- Lazy load images
- Responsive images (`srcset`)

### Bundle Size

- Enable compression in production
- Tree shaking unused code
- Minimize dependencies

## Testing

### Unit Tests (Blazor Components)

Use bUnit for component testing:

```csharp
[Fact]
public void GameSessionPage_WithValidSession_DisplaysScene()
{
    // Arrange
    var ctx = new TestContext();
    var mockClient = new Mock<IGameSessionApiClient>();
    mockClient.Setup(c => c.GetSessionAsync("123"))
        .ReturnsAsync(new GameSession { /* ... */ });

    ctx.Services.AddSingleton(mockClient.Object);

    // Act
    var component = ctx.RenderComponent<GameSessionPage>(
        parameters => parameters.Add(p => p.SessionId, "123"));

    // Assert
    component.Find("h1").TextContent.Should().Contain("The Enchanted Forest");
}
```

### Integration Tests

Test API client integration:

```csharp
[Fact]
public async Task ScenarioClient_GetScenarios_ReturnsScenarios()
{
    var client = new ScenarioApiClient(httpClient, logger);
    var scenarios = await client.GetScenariosAsync();

    Assert.NotEmpty(scenarios);
}
```

## Deployment

### Azure Static Web Apps (Recommended)

1. **Build**: `dotnet publish -c Release`
2. **Deploy**: GitHub Actions automatically deploys
3. **CDN**: Global content delivery
4. **Custom Domain**: Configure DNS

### GitHub Actions Workflow

```yaml
- name: Build Blazor PWA
  run: dotnet publish src/Mystira.App.PWA/Mystira.App.PWA.csproj -c Release -o ./publish

- name: Deploy to Azure Static Web Apps
  uses: Azure/static-web-apps-deploy@v1
  with:
    app_location: "./publish/wwwroot"
```

## Future Enhancements

- **Push Notifications**: Real-time game updates
- **Background Sync**: Offline choice sync
- **Camera Integration**: Upload custom character images
- **Voice Input**: Voice-controlled gameplay
- **Multiplayer**: Real-time co-op sessions

## Related Documentation

- **[API](../Mystira.App.Api/README.md)** - Backend API consumed by PWA
- **[Domain](../Mystira.App.Domain/README.md)** - Shared domain models
- **[Main README](../../README.md)** - Project overview

## License

Copyright (c) 2025 Mystira. All rights reserved.
