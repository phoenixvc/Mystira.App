# UX Components Usage Guide

**Created:** November 24, 2025
**Components:** LoadingIndicator, ErrorBoundaryWrapper, ToastService/ToastContainer

This guide shows how to use the newly implemented UX components to improve user experience across the Mystira PWA.

---

## 1. LoadingIndicator Component

### Purpose
Provides consistent loading states with spinner or skeleton placeholders.

### Usage Examples

#### Basic Spinner
```razor
@if (isLoading)
{
    <LoadingIndicator Message="Loading adventures..." />
}
else
{
    @* Your content here *@
}
```

####  Skeleton Loading
```razor
@if (isLoading)
{
    <LoadingIndicator ShowSpinner="false" ShowSkeleton="true" SkeletonCount="5" />
}
else
{
    @foreach (var scenario in scenarios)
    {
        <ScenarioCard Scenario="scenario" />
    }
}
```

#### Custom Styling
```razor
<LoadingIndicator
    ShowSpinner="true"
    Message="Please wait while we prepare your session..."
    CssClass="my-custom-class" />
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ShowSpinner` | bool | true | Show rotating spinner |
| `ShowSkeleton` | bool | false | Show skeleton placeholders |
| `SkeletonCount` | int | 3 | Number of skeleton items |
| `Message` | string? | null | Optional message below spinner |
| `CssClass` | string? | null | Custom CSS class |

---

## 2. ErrorBoundaryWrapper Component

### Purpose
Gracefully handle errors and prevent full app crashes with recovery options.

### Usage Examples

#### Basic Error Boundary
```razor
<ErrorBoundaryWrapper>
    <ChildContent>
        @* Component that might throw errors *@
        <ComplexGameComponent />
    </ChildContent>
</ErrorBoundaryWrapper>
```

#### Custom Error Messages
```razor
<ErrorBoundaryWrapper
    ErrorTitle="Oops! Adventure failed to load"
    ErrorMessage="We couldn't load this adventure. Please try again or choose a different one."
    RecoverButtonText="Try Another Adventure"
    OnRecover="HandleRecovery">
    <ChildContent>
        <GameSessionPage />
    </ChildContent>
</ErrorBoundaryWrapper>
```

#### Show Technical Details (Development)
```razor
<ErrorBoundaryWrapper ShowDetails="true" ShowReloadButton="true">
    <ChildContent>
        <AdminPanel />
    </ChildContent>
</ErrorBoundaryWrapper>
```

### Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| `ChildContent` | RenderFragment | - | Content to wrap with error boundary |
| `ErrorTitle` | string | "Something went wrong" | Custom error title |
| `ErrorMessage` | string | Default message | Custom error message |
| `RecoverButtonText` | string | "Try Again" | Text for recover button |
| `ShowDetails` | bool | false | Show technical error details |
| `ShowReloadButton` | bool | true | Show page reload button |
| `OnRecover` | EventCallback | - | Callback when user clicks recover |

---

## 3. ToastService & ToastContainer

### Purpose
Display temporary notifications for user feedback on actions (success, error, warning, info).

### Setup (Already Done)

The `ToastContainer` is already added to `MainLayout.razor`, and `ToastService` is registered in `Program.cs`.

### Usage Examples

#### Success Notification
```razor
@inject ToastService ToastService

<button @onclick="SaveProfile">Save Profile</button>

@code {
    private async Task SaveProfile()
    {
        try
        {
            await ProfileService.SaveAsync(profile);
            ToastService.ShowSuccess("Profile saved successfully!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save profile: {ex.Message}");
        }
    }
}
```

#### Error Notification
```razor
ToastService.ShowError("Failed to load scenarios. Please try again.");
```

#### Warning Notification
```razor
ToastService.ShowWarning("Your session will expire in 5 minutes.");
```

#### Info Notification
```razor
ToastService.ShowInfo("New adventure available!");
```

#### Custom Duration
```razor
// Show for 10 seconds instead of default
ToastService.ShowSuccess("Achievement unlocked!", durationMs: 10000);
```

#### Clear All Toasts
```razor
ToastService.Clear();
```

### Methods

| Method | Parameters | Default Duration | Description |
|--------|------------|------------------|-------------|
| `ShowSuccess` | message, durationMs | 3000ms | Green success toast |
| `ShowError` | message, durationMs | 5000ms | Red error toast |
| `ShowWarning` | message, durationMs | 4000ms | Amber warning toast |
| `ShowInfo` | message, durationMs | 3000ms | Blue info toast |
| `Show` | message, type, durationMs | 3000ms | Custom type toast |
| `Clear` | - | - | Clear all toasts |

---

## 4. Real-World Integration Examples

### Example 1: Loading & Error in Scenario Page

```razor
@page "/scenarios"
@inject IScenarioApiClient ScenarioApi
@inject ToastService ToastService

<ErrorBoundaryWrapper
    ErrorTitle="Scenarios unavailable"
    ErrorMessage="We couldn't load the scenario list. Please refresh the page."
    OnRecover="LoadScenariosAsync">
    <ChildContent>
        @if (isLoading)
        {
            <LoadingIndicator
                ShowSkeleton="true"
                SkeletonCount="6"
                Message="Loading epic adventures..." />
        }
        else if (scenarios.Any())
        {
            <div class="scenario-grid">
                @foreach (var scenario in scenarios)
                {
                    <ScenarioCard Scenario="scenario" />
                }
            </div>
        }
        else
        {
            <EmptyState Message="No scenarios available" />
        }
    </ChildContent>
</ErrorBoundaryWrapper>

@code {
    private List<Scenario> scenarios = new();
    private bool isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        await LoadScenariosAsync();
    }

    private async Task LoadScenariosAsync()
    {
        try
        {
            isLoading = true;
            StateHasChanged();

            scenarios = await ScenarioApi.GetAllScenariosAsync();

            ToastService.ShowSuccess($"Loaded {scenarios.Count} adventures!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load scenarios");
            throw; // ErrorBoundary will catch this
        }
        finally
        {
            isLoading = false;
            StateHasChanged();
        }
    }
}
```

### Example 2: Form Submission with Feedback

```razor
@inject ToastService ToastService
@inject IUserProfileApiClient ProfileApi

<EditForm Model="profileModel" OnValidSubmit="HandleSubmitAsync">
    <DataAnnotationsValidator />

    <InputText @bind-Value="profileModel.DisplayName" />
    <ValidationMessage For="() => profileModel.DisplayName" />

    <button type="submit" disabled="@isSubmitting">
        @if (isSubmitting)
        {
            <span class="loading-spinner-small"></span>
            <span>Saving...</span>
        }
        else
        {
            <span>Save Profile</span>
        }
    </button>
</EditForm>

@code {
    private ProfileModel profileModel = new();
    private bool isSubmitting = false;

    private async Task HandleSubmitAsync()
    {
        try
        {
            isSubmitting = true;

            await ProfileApi.UpdateProfileAsync(profileModel);

            ToastService.ShowSuccess("Profile updated successfully!");
        }
        catch (HttpRequestException)
        {
            ToastService.ShowError("Network error. Please check your connection.");
        }
        catch (Exception ex)
        {
            ToastService.ShowError($"Failed to save: {ex.Message}");
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

### Example 3: Game Session with Multiple States

```razor
@page "/session/{SessionId}"
@inject IGameSessionService GameSessionService
@inject ToastService ToastService

<ErrorBoundaryWrapper>
    <ChildContent>
        @if (isLoading)
        {
            <LoadingIndicator Message="Loading your adventure..." />
        }
        else if (session == null)
        {
            <div class="alert alert-warning">
                Session not found.
                <a href="/">Return to adventures</a>
            </div>
        }
        else if (isProcessingChoice)
        {
            <LoadingIndicator
                Message="Processing your choice..."
                ShowSkeleton="false" />
        }
        else
        {
            @* Render game session UI *@
            <GameSessionComponent
                Session="session"
                OnChoiceSelected="HandleChoiceAsync" />
        }
    </ChildContent>
</ErrorBoundaryWrapper>

@code {
    [Parameter] public string SessionId { get; set; } = default!;

    private GameSession? session;
    private bool isLoading = true;
    private bool isProcessingChoice = false;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            session = await GameSessionService.LoadSessionAsync(SessionId);
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to load session");
            throw; // Let ErrorBoundary handle it
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task HandleChoiceAsync(Choice choice)
    {
        try
        {
            isProcessingChoice = true;

            await GameSessionService.ProcessChoiceAsync(SessionId, choice);

            ToastService.ShowInfo("Your choice has shaped the story!");
        }
        catch (Exception ex)
        {
            ToastService.ShowError("Failed to process choice. Please try again.");
        }
        finally
        {
            isProcessingChoice = false;
        }
    }
}
```

---

## 5. Accessibility Features

All components are built with accessibility in mind:

### LoadingIndicator
- `role="status"` for screen readers
- `aria-live="polite"` for status updates
- Reduced motion support (slower animations)

### ErrorBoundaryWrapper
- `role="alert"` for errors
- `aria-live="assertive"` for critical errors
- Keyboard accessible recovery buttons
- Focus management on error state

### ToastContainer
- `aria-live="polite"` for non-intrusive notifications
- `aria-atomic="true"` for complete message reading
- Close button with `aria-label`
- Keyboard accessible (Tab + Enter to dismiss)

---

## 6. Best Practices

### When to use LoadingIndicator

✅ **Use for:**
- Initial page loads
- Data fetching operations
- Form submissions
- File uploads

❌ **Don't use for:**
- Instant operations (< 100ms)
- Background sync operations
- Micro-interactions (button clicks without data)

### When to use ErrorBoundaryWrapper

✅ **Use for:**
- Complex components that might fail
- Third-party integrations
- Data-heavy pages
- Critical user flows (game sessions, payments)

❌ **Don't use for:**
- Every single component (too granular)
- Simple static content
- Already-handled errors (try-catch is sufficient)

### When to use ToastService

✅ **Use for:**
- Success confirmations
- Non-critical errors
- Warnings and info messages
- Temporary feedback

❌ **Don't use for:**
- Critical errors (use modals or ErrorBoundary)
- Persistent messages (use alerts or banners)
- Form validation errors (use inline validation)

---

## 7. Performance Considerations

### LoadingIndicator
- Lightweight (~2KB CSS)
- No JavaScript dependencies
- Uses CSS animations (GPU-accelerated)
- Supports reduced motion preferences

### ErrorBoundaryWrapper
- Negligible overhead when no error
- Lazy-loads error UI only when needed
- Cleans up resources on disposal

### ToastService
- Efficient event-based system
- Auto-cleanup with timers
- Maximum 5 concurrent toasts recommended
- Supports disposal pattern

---

## 8. Testing

### Example Unit Test (xUnit)

```csharp
public class ToastServiceTests
{
    [Fact]
    public void ShowSuccess_RaisesOnShowEvent()
    {
        // Arrange
        var toastService = new ToastService();
        ToastMessage? capturedMessage = null;
        toastService.OnShow += (message) => capturedMessage = message;

        // Act
        toastService.ShowSuccess("Test message");

        // Assert
        Assert.NotNull(capturedMessage);
        Assert.Equal("Test message", capturedMessage.Message);
        Assert.Equal(ToastType.Success, capturedMessage.Type);
        Assert.Equal(3000, capturedMessage.DurationMs);
    }

    [Fact]
    public void Clear_RaisesOnClearEvent()
    {
        // Arrange
        var toastService = new ToastService();
        bool clearCalled = false;
        toastService.OnClear += () => clearCalled = true;

        // Act
        toastService.Clear();

        // Assert
        Assert.True(clearCalled);
    }
}
```

---

## 9. Migration Guide

### Before (Old Pattern)
```razor
@if (loading)
{
    <div class="spinner-border"></div>
    <p>Loading...</p>
}
```

### After (New Pattern)
```razor
@if (loading)
{
    <LoadingIndicator Message="Loading..." />
}
```

---

## 10. Troubleshooting

### Toast not showing?
- ✅ Verify `ToastService` is injected: `@inject ToastService ToastService`
- ✅ Check `ToastContainer` is in `MainLayout.razor`
- ✅ Ensure service is registered in `Program.cs`

### Error boundary not catching errors?
- ✅ Error must be thrown during render (not in `OnInitializedAsync` unless rethrown)
- ✅ Wrap the component that throws, not the parent
- ✅ Check browser console for unhandled errors

### Loading indicator not animating?
- ✅ Check for `prefers-reduced-motion` setting in browser
- ✅ Verify CSS is loaded (check browser DevTools)
- ✅ Ensure scoped CSS is not being stripped by build

---

**Questions or Issues?**
See `PRODUCTION_REVIEW_REPORT_UPDATED.md` for comprehensive implementation tracking.
