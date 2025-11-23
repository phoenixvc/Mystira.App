# PendingSignup Domain Model

## Overview

The `PendingSignup` domain model represents a passwordless authentication signup or signin request. It stores temporary signup codes and tracks their usage and expiration.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/PendingSignup.cs`

## Properties

| Property         | Type       | Description                            |
| ---------------- | ---------- | -------------------------------------- |
| `Id`             | `string`   | Unique identifier (GUID)               |
| `Email`          | `string`   | Email address for signup/signin        |
| `DisplayName`    | `string`   | Display name for the account           |
| `Code`           | `string`   | Verification code sent via email       |
| `CreatedAt`      | `DateTime` | Creation timestamp                     |
| `ExpiresAt`      | `DateTime` | Expiration timestamp                   |
| `IsUsed`         | `bool`     | Whether the code has been used         |
| `IsSignin`       | `bool`     | True for signin, false for signup      |
| `FailedAttempts` | `int`      | Number of failed verification attempts |

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `CreatePendingSignupUseCase` - Create signup request
- ❌ `GetPendingSignupUseCase` - Get signup by code
- ❌ `ValidatePendingSignupUseCase` - Validate signup code
- ❌ `CompletePendingSignupUseCase` - Complete signup process
- ❌ `ExpirePendingSignupUseCase` - Mark signup as expired

**Current Implementation**: `PasswordlessAuthService` (should be refactored)

**Recommendation**: Create `Application.UseCases.Authentication` directory

## Passwordless Authentication Flow

1. **Create Signup Request**: User provides email, system creates `PendingSignup` with code
2. **Send Code**: Code is sent via email to user
3. **Validate Code**: User enters code, system validates against `PendingSignup`
4. **Complete Signup**: On successful validation, account is created and `PendingSignup` is marked as used
5. **Expiration**: Expired or used signups are cleaned up periodically

## Security Considerations

- Codes expire after a set time period (`ExpiresAt`)
- Failed attempts are tracked (`FailedAttempts`) to prevent brute force
- Codes are single-use (`IsUsed` flag)
- Signup vs signin distinction (`IsSignin` flag)

## Persistence

- Stored in Cosmos DB via `IPendingSignupRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id` and `Code`
- Should be cleaned up periodically (expired signups)

## Related Documentation

- [Account Domain Model](./account.md)
- [Use Cases Documentation](../usecases/README.md)
