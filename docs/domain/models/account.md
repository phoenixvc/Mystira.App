# Account Domain Model

## Overview

The `Account` domain model represents a user account with authentication, subscription management, and profile associations. Accounts are the top-level entity for user management and can have multiple user profiles.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/Account.cs`

## Properties

| Property                | Type                      | Description                                    |
| ----------------------- | ------------------------- | ---------------------------------------------- |
| `Id`                    | `string`                  | Unique identifier (GUID)                      |
| `Auth0UserId`           | `string`                  | Auth0 user identifier                         |
| `Email`                  | `string`                  | Account email address                         |
| `DisplayName`           | `string`                  | Display name for the account                  |
| `Role`                  | `string`                  | User role (default: "Guest", can be "Admin")  |
| `UserProfileIds`        | `List<string>`            | IDs of user profiles associated with account  |
| `CompletedScenarioIds`  | `List<string>`            | IDs of scenarios completed by this account   |
| `Subscription`          | `SubscriptionDetails`     | Subscription information                      |
| `Settings`              | `AccountSettings`         | Account settings                              |
| `CreatedAt`             | `DateTime`                | Account creation timestamp                     |
| `LastLoginAt`           | `DateTime`                | Last login timestamp                          |

## Related Domain Models

### SubscriptionDetails

Represents subscription and purchase information for the account.

**Properties**:

| Property              | Type                | Description                                    |
| --------------------- | ------------------- | ---------------------------------------------- |
| `Type`                | `SubscriptionType`  | Subscription type (Free, Monthly, Annual, etc.) |
| `ProductId`           | `string`            | App store product identifier                  |
| `ValidUntil`          | `DateTime?`         | Subscription expiration (null for lifetime)   |
| `IsActive`            | `bool`              | Whether subscription is active                |
| `PurchaseToken`       | `string?`           | App store purchase verification token         |
| `LastVerified`        | `DateTime?`         | Last subscription verification timestamp      |
| `PurchasedScenarios`  | `List<string>`      | IDs of individually purchased scenarios       |

**Methods**:

- `IsSubscriptionActive()` - Returns `true` if subscription is active and not expired

### AccountSettings

Represents user preferences and account settings.

**Properties**:

| Property                  | Type     | Description                                    |
| ------------------------- | -------- | ---------------------------------------------- |
| `CacheCredentials`        | `bool`   | Whether to cache credentials (default: true)   |
| `RequireAuthOnStartup`    | `bool`   | Whether to require auth on startup (default: false) |
| `PreferredLanguage`       | `string` | Preferred language code (default: "en")       |
| `NotificationsEnabled`    | `bool`   | Whether notifications are enabled (default: true) |

### SubscriptionType Enum

Enumeration of subscription types:

- `Free` - Limited access, no subscription
- `Monthly` - Monthly subscription
- `Annual` - Annual subscription
- `Lifetime` - One-time purchase with lifetime updates
- `Individual` - Individual scenario purchases

## Relationships

- `Account` → `List<UserProfile>` (via `UserProfileIds`)
- `Account` → `List<Scenario>` (via `CompletedScenarioIds`)
- `Account` → `List<GameSession>` (via `AccountId`)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `GetAccountUseCase` - Get account by ID
- ❌ `GetAccountByEmailUseCase` - Get account by email
- ❌ `CreateAccountUseCase` - Create new account
- ❌ `UpdateAccountUseCase` - Update account details
- ❌ `UpdateAccountSettingsUseCase` - Update account settings
- ❌ `UpdateSubscriptionUseCase` - Update subscription details
- ❌ `AddUserProfileToAccountUseCase` - Link profile to account
- ❌ `RemoveUserProfileFromAccountUseCase` - Unlink profile from account
- ❌ `AddCompletedScenarioUseCase` - Mark scenario as completed
- ❌ `GetCompletedScenariosUseCase` - Get completed scenarios

**Current Implementation**: `AccountApiService` (should be refactored)

**Recommendation**: Create `Application.UseCases.Accounts` directory and migrate service logic

## Persistence

- Stored in Cosmos DB via `IAccountRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id`, `Auth0UserId`, and `Email`

## Related Documentation

- [UserProfile Domain Model](./user-profile.md)
- [GameSession Domain Model](./game-session.md)
- [Use Cases Documentation](../usecases/README.md)
