# ContentBundle Domain Model

## Overview

The `ContentBundle` domain model represents collections of scenarios that can be purchased or accessed together. Bundles support age-group targeting and pricing information.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/ContentBundle.cs`

## Properties

| Property      | Type                | Description                                  |
| ------------- | ------------------- | -------------------------------------------- |
| `Id`          | `string`            | Unique identifier (GUID, format: "N")        |
| `Title`       | `string`            | Bundle title                                 |
| `Description` | `string`            | Bundle description                           |
| `ScenarioIds` | `List<string>`      | IDs of scenarios included in bundle          |
| `ImageId`     | `string`            | Image path (used by CachedMystiraImage)      |
| `Prices`      | `List<BundlePrice>` | Pricing information for different currencies |
| `IsFree`      | `bool`              | Whether bundle is free                       |
| `AgeGroup`    | `string`            | Target age group                             |

## Related Domain Models

### BundlePrice

Represents pricing information for a bundle in a specific currency.

**Properties**:

| Property   | Type      | Description                    |
| ---------- | --------- | ------------------------------ |
| `Value`    | `decimal` | Price value                    |
| `Currency` | `string`  | Currency code (default: "USD") |

## Relationships

- `ContentBundle` → `List<Scenario>` (via `ScenarioIds`)
- `ContentBundle` → `Account` (via subscription/purchase access)
- `ContentBundle` → `AgeGroup` (via `AgeGroup`)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `GetContentBundlesUseCase` - Get all bundles
- ❌ `GetContentBundleUseCase` - Get bundle by ID
- ❌ `GetContentBundlesByAgeGroupUseCase` - Get bundles for age group
- ❌ `CreateContentBundleUseCase` - Create new bundle
- ❌ `UpdateContentBundleUseCase` - Update bundle
- ❌ `DeleteContentBundleUseCase` - Delete bundle
- ❌ `AddScenarioToBundleUseCase` - Add scenario to bundle
- ❌ `RemoveScenarioFromBundleUseCase` - Remove scenario from bundle
- ❌ `CheckBundleAccessUseCase` - Check if account has access to bundle

**Current Implementation**: `ContentBundleService` (should be refactored)

**Recommendation**: Create `Application.UseCases.ContentBundles` directory

## Bundle Access Logic

Bundle access is determined by:

1. **Free Bundles**: `IsFree = true` - accessible to all accounts
2. **Purchased Bundles**: Account has bundle ID in subscription/purchase list
3. **Subscription Bundles**: Account has active subscription that includes bundle
4. **Age Group**: Bundle `AgeGroup` matches user profile age group

## Persistence

- Stored in Cosmos DB via `IContentBundleRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id` and `AgeGroup`
- Can be queried by age group for content filtering

## Related Documentation

- [Scenario Domain Model](./scenario.md)
- [Account Domain Model](./account.md)
- [UserProfile Domain Model](./user-profile.md)
- [Use Cases Documentation](../usecases/README.md)
