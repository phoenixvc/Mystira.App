# API Service Layer Refactoring Strategy

## Overview

This document outlines the strategy for refactoring API and Admin.Api service layers to properly implement hexagonal architecture by delegating to Application use cases instead of directly accessing infrastructure.

## Current State (After Phase 3)

✅ **Completed:**
- Phase 1: Repository interfaces moved to Application/Ports/Data
- Phase 2: Azure & Discord port interfaces moved to Application/Ports
- Phase 3: Application layer has ZERO infrastructure dependencies

❌ **Remaining Issues:**
- **API Layer**: 47 service files directly accessing infrastructure (repositories, UnitOfWork)
- **Admin.Api Layer**: 41 service files directly accessing infrastructure
- **Total**: 88 service files violating hexagonal architecture

## Problem

API services are currently:
1. ❌ Directly injecting and using `IRepository` interfaces
2. ❌ Directly injecting and using `IUnitOfWork`
3. ❌ Containing business logic that belongs in Application use cases
4. ❌ Tightly coupled to infrastructure implementation details

Example (BEFORE):
```csharp
public class AccountApiService
{
    private readonly IAccountRepository _repository;  // ❌ Direct infrastructure access
    private readonly IUnitOfWork _unitOfWork;          // ❌ Direct infrastructure access

    public async Task<Account> CreateAccountAsync(Account account)
    {
        // ❌ Business logic in API layer
        if (string.IsNullOrEmpty(account.Id))
            account.Id = Guid.NewGuid().ToString();

        await _repository.AddAsync(account);
        await _unitOfWork.SaveChangesAsync();
        return account;
    }
}
```

## Solution

API services should be thin wrappers that delegate to Application use cases:

Example (AFTER):
```csharp
public class AccountApiService
{
    private readonly CreateAccountUseCase _createAccountUseCase;  // ✅ Use case injection

    public async Task<Account> CreateAccountAsync(Account account)
    {
        var request = new CreateAccountRequest
        {
            Email = account.Email,
            DisplayName = account.DisplayName
        };

        return await _createAccountUseCase.ExecuteAsync(request);  // ✅ Delegate to use case
    }
}
```

## Refactoring Steps

### Step 1: Identify Service Methods

For each API service file:
1. List all public methods
2. Identify which use cases already exist
3. Identify which use cases need to be created

### Step 2: Create Missing Use Cases (if needed)

If a use case doesn't exist:
1. Create it in `Application/UseCases/[Domain]/`
2. Follow existing use case patterns
3. Inject only Application ports (never Infrastructure)
4. Keep business logic in the use case

### Step 3: Refactor Service

For each service method:
1. Remove repository/infrastructure dependencies from constructor
2. Add use case dependencies instead
3. Convert method parameters to request DTOs (if needed)
4. Call use case ExecuteAsync() method
5. Handle exceptions appropriately

### Step 4: Update DI Registrations

Ensure all use cases are registered in Program.cs or ServiceCollectionExtensions

## Service Categorization

### Category A: Services with Existing Use Cases (Easy)

These can be refactored immediately by simply calling existing use cases:

- **AccountApiService** ✅ (Example completed - see src/Mystira.App.Api/Services/AccountApiService.cs)
  - GetAccountByEmailUseCase ✅
  - GetAccountUseCase ✅
  - CreateAccountUseCase ✅
  - UpdateAccountUseCase ✅
  - AddCompletedScenarioUseCase ✅

- **ScenarioApiService**
  - GetScenarioUseCase ✅
  - GetScenariosUseCase ✅
  - CreateScenarioUseCase ✅
  - UpdateScenarioUseCase ✅
  - DeleteScenarioUseCase ✅

- **GameSessionApiService**
  - CreateGameSessionUseCase ✅
  - GetGameSessionUseCase ✅
  - EndGameSessionUseCase ✅
  - MakeChoiceUseCase ✅
  - ProgressSceneUseCase ✅
  - (and more...)

- **UserProfileApiService**
  - CreateUserProfileUseCase ✅
  - GetUserProfileUseCase ✅
  - UpdateUserProfileUseCase ✅
  - DeleteUserProfileUseCase ✅

- **ContentBundleService / BundleService**
  - CreateContentBundleUseCase ✅
  - GetContentBundleUseCase ✅
  - UpdateContentBundleUseCase ✅
  - DeleteContentBundleUseCase ✅

- **MediaApiService / MediaUploadService**
  - UploadMediaUseCase ✅
  - DeleteMediaUseCase ✅
  - GetMediaUseCase ✅
  - (and more...)

### Category B: Services Needing New Use Cases (Medium)

These require creating new use cases first:

- **CharacterMapApiService**
  - Need: ImportCharacterMapUseCase, ExportCharacterMapUseCase (partially exist)

- **AvatarApiService**
  - Need: GetAvatarsByAgeGroupUseCase (exists)
  - Need: AssignAvatarToAgeGroupUseCase (exists)

- **BadgeConfigurationApiService**
  - Need: ImportBadgeConfigurationUseCase (exists)
  - Need: ExportBadgeConfigurationUseCase (exists)

### Category C: Infrastructure Services (Keep as-is)

These are legitimately infrastructure concerns and should remain in API layer:

- **JwtService** - Token generation (infrastructure)
- **AzureEmailService / IEmailService** - Email sending (infrastructure)
- **PasswordlessAuthService** - Authentication flow (infrastructure/framework)
- **AppStatusService / HealthCheckService** - Monitoring (infrastructure)
- **ClientStatusService / ClientApiService** - Connection management (infrastructure)

## Example: Complete AccountApiService Refactoring

See `src/Mystira.App.Api/Services/AccountApiService.cs` for a complete example of:
- ✅ Removing all Infrastructure dependencies
- ✅ Injecting only Use Cases
- ✅ Thin wrapper methods that delegate to use cases
- ✅ Proper exception handling

## Bulk Refactoring Script (Optional)

For services that follow a simple pattern, consider creating a code generator or bulk refactoring script:

```bash
# Example: Find all services still using Infrastructure namespaces
grep -r "using Mystira.App.Infrastructure" src/Mystira.App.Api/Services/ --include="*.cs"
grep -r "using Mystira.App.Infrastructure" src/Mystira.App.Admin.Api/Services/ --include="*.cs"
```

## Migration Checklist

For each service file:

- [ ] Analyze methods and identify corresponding use cases
- [ ] Create missing use cases (if needed)
- [ ] Update service constructor to inject use cases
- [ ] Refactor each method to call use cases
- [ ] Remove Infrastructure using statements
- [ ] Update DI registrations
- [ ] Test the refactored service

## Success Criteria

When Phase 4 & 5 are complete:

✅ API services contain NO infrastructure namespace imports
✅ API services inject only Application use cases
✅ API services are thin wrappers (< 10 lines per method)
✅ All business logic is in Application use cases
✅ Clean separation: Controllers → Services → Use Cases → Ports → Infrastructure

## Timeline Estimate

- **Category A Services (with existing use cases)**: ~30 services × 30 min = 15 hours
- **Category B Services (need new use cases)**: ~10 services × 2 hours = 20 hours
- **Testing and verification**: 5 hours
- **Total**: ~40 hours (1 week of focused work)

## Recommendation

Given the scope, consider:
1. Refactoring incrementally (one domain at a time)
2. Starting with high-traffic/critical services first
3. Creating automated tests for each refactored service
4. Doing this work across multiple sprints/PRs

## Reference Implementation

✅ **AccountApiService** - Fully refactored example (see src/Mystira.App.Api/Services/AccountApiService.cs)

This demonstrates the complete pattern and can serve as a template for refactoring other services.
