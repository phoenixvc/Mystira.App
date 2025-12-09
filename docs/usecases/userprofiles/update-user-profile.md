# Update User Profile Use Case

## Overview

The `UpdateUserProfileUseCase` updates an existing user profile with partial updates and validation.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.UserProfiles.UpdateUserProfileUseCase`

**Input**: `string id`, `UpdateUserProfileRequest`

**Output**: `UserProfile?` (updated domain model, null if not found)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as UserProfilesController
    participant Service as UserProfileApiService
    participant UseCase as UpdateUserProfileUseCase
    participant Repo as IUserProfileRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: PUT /api/userprofiles/{id}<br/>(UpdateUserProfileRequest)
    Controller->>Service: UpdateProfileAsync(id, request)
    
    Service->>UseCase: ExecuteAsync(id, request)
    
    Note over UseCase: Step 1: Find Existing Profile
    UseCase->>Repo: GetByIdAsync(id)
    Repo->>DB: Query profile
    alt Profile Not Found
        DB-->>Repo: null
        Repo-->>UseCase: null
        UseCase-->>Service: null
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    end
    DB-->>Repo: UserProfile
    Repo-->>UseCase: existingProfile
    
    Note over UseCase: Step 2: Apply Partial Updates
    alt PreferredFantasyThemes Provided
        UseCase->>UseCase: Validate themes<br/>(FantasyTheme.Parse)
        alt Invalid Themes
            UseCase-->>Service: ArgumentException
            Service-->>Controller: BadRequest
            Controller-->>Client: 400 Bad Request
        end
        UseCase->>UseCase: profile.PreferredFantasyThemes =<br/>  (parsed themes)
    end
    
    alt AgeGroup Provided
        UseCase->>UseCase: Validate age group<br/>(in AllAgeGroups)
        alt Invalid Age Group
            UseCase-->>Service: ArgumentException
            Service-->>Controller: BadRequest
            Controller-->>Client: 400 Bad Request
        end
        UseCase->>UseCase: profile.AgeGroupName =<br/>  request.AgeGroup
    end
    
    alt DateOfBirth Provided
        UseCase->>UseCase: profile.DateOfBirth =<br/>  request.DateOfBirth
        UseCase->>UseCase: profile.UpdateAgeGroupFromBirthDate()
    end
    
    alt Other Fields Provided
        UseCase->>UseCase: Update HasCompletedOnboarding,<br/>IsGuest, IsNpc, AccountId,<br/>Pronouns, Bio (if provided)
    end
    
    UseCase->>UseCase: profile.UpdatedAt = Now
    
    Note over UseCase: Step 3: Persist Changes
    UseCase->>Repo: UpdateAsync(profile)
    Repo->>DB: Update entity
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    Repo-->>UseCase: UserProfile (updated)
    
    UseCase-->>Service: UserProfile
    Service-->>Controller: UserProfile
    Controller-->>Client: 200 OK<br/>(UserProfile)
```

## Partial Update Support

All fields in `UpdateUserProfileRequest` are optional:

- Only provided fields are updated
- Null fields are ignored
- Validation only applies to provided fields

## Update Fields

### Validated Fields

- **PreferredFantasyThemes**: Validated against `FantasyTheme` domain model
- **AgeGroup**: Validated against `AgeGroupConstants.AllAgeGroups`
- **DateOfBirth**: Triggers automatic age group update

### Direct Update Fields

- **HasCompletedOnboarding**: Boolean flag
- **IsGuest**: Guest profile flag
- **IsNpc**: NPC profile flag
- **AccountId**: Parent account ID
- **Pronouns**: User pronouns
- **Bio**: Profile biography

### Auto-Updated Fields

- **UpdatedAt**: Always set to current UTC time

## Age Group Auto-Update

If `DateOfBirth` is provided:

- Updates `DateOfBirth` property
- Calls `UpdateAgeGroupFromBirthDate()` domain method
- Automatically calculates and updates age group based on age

## Validation

Same validation as creation:

- Fantasy themes must parse to valid `FantasyTheme` objects
- Age group must be in valid age groups list
- Date of birth triggers age group recalculation

## Error Handling

- **Profile Not Found**: Returns `null` (handled as 404)
- **Invalid Fantasy Themes**: Returns `ArgumentException` with invalid themes
- **Invalid Age Group**: Returns `ArgumentException` with valid options
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create User Profile Use Case](./create-user-profile.md)
- [User Profile Domain Model](../../domain/models/user-profile.md)
