# Contracts Migration Plan

This document lists types currently defined locally in `Mystira.App.Api/Models/ContractModels.cs` that should be migrated to the `Mystira.Contracts` workspace package for shared use across all APIs and clients.

## Types to Migrate

### Common Response Models

These models are used across multiple APIs for consistent error handling and validation responses.

| Type | Current Location | Target Namespace |
|------|-----------------|------------------|
| `ErrorResponse` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Responses` |
| `ValidationResult` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Responses` |
| `HealthCheckResponse` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Responses` |

### Royalty Request Models

These models are used for Story Protocol royalty management.

| Type | Current Location | Target Namespace |
|------|-----------------|------------------|
| `PayRoyaltyRequest` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Requests.Royalties` |
| `ClaimRoyaltiesRequest` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Requests.Royalties` |

### Game Session Request Models

| Type | Current Location | Target Namespace |
|------|-----------------|------------------|
| `CompleteScenarioRequest` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Requests.GameSessions` |

### User Profile Request Models

| Type | Current Location | Target Namespace |
|------|-----------------|------------------|
| `ProfileAssignmentRequest` | `Mystira.App.Api.Models` | `Mystira.Contracts.App.Requests.UserProfiles` |

## Migration Steps

1. Add the new types to `Mystira.Contracts` package under appropriate namespaces
2. Update the package version in `Mystira.Contracts.csproj`
3. Publish the updated package to GitHub Packages
4. Update all API projects to reference the new package version
5. Remove the local definitions from `ContractModels.cs`
6. Update import statements in controllers to use the Contracts namespace

## Notes

- The `AgeGroupDefinition` and `ArchetypeDefinition` types should remain in `Mystira.App.Domain.Models` as they are domain entities, not DTOs
- Request/Response types that cross API boundaries should be in Contracts
- Internal domain models should stay in the Domain project
