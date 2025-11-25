# Create User Profile Use Case

## Overview

The `CreateUserProfileUseCase` handles creation of new user profiles with comprehensive validation.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.UserProfiles.CreateUserProfileUseCase`

**Input**: `CreateUserProfileRequest`

**Output**: `UserProfile` (domain model)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as UserProfilesController
    participant Service as UserProfileApiService
    participant UseCase as CreateUserProfileUseCase
    participant Repo as IUserProfileRepository
    participant UoW as IUnitOfWork
    participant DB as CosmosDB

    Client->>Controller: POST /api/userprofiles<br/>(CreateUserProfileRequest)
    Controller->>Service: CreateProfileAsync(request)
    
    Service->>UseCase: ExecuteAsync(request)
    
    Note over UseCase: Step 1: Check Existing Profile
    UseCase->>Repo: GetByIdAsync(request.Id)
    Repo->>DB: Query profile
    alt Profile Already Exists
        DB-->>Repo: UserProfile
        Repo-->>UseCase: existingProfile
        UseCase-->>Service: ArgumentException("Profile already exists")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    DB-->>Repo: null
    Repo-->>UseCase: null
    
    Note over UseCase: Step 2: Validate Fantasy Themes
    loop For each theme in request.PreferredFantasyThemes
        UseCase->>UseCase: FantasyTheme.Parse(theme)
        alt Invalid Theme
            UseCase-->>Service: ArgumentException("Invalid fantasy themes")
            Service-->>Controller: BadRequest
            Controller-->>Client: 400 Bad Request
        end
    end
    
    Note over UseCase: Step 3: Validate Age Group
    UseCase->>UseCase: Check if request.AgeGroup<br/>in AgeGroupConstants.AllAgeGroups
    alt Invalid Age Group
        UseCase-->>Service: ArgumentException("Invalid age group")
        Service-->>Controller: BadRequest
        Controller-->>Client: 400 Bad Request
    end
    
    Note over UseCase: Step 4: Create Domain Model
    UseCase->>UseCase: new UserProfile {<br/>  Id = request.Id,<br/>  Name = request.Name,<br/>  AccountId = request.AccountId,<br/>  PreferredFantasyThemes =<br/>    (parsed FantasyTheme list),<br/>  AgeGroupName = request.AgeGroup,<br/>  DateOfBirth = request.DateOfBirth,<br/>  IsGuest = request.IsGuest,<br/>  IsNpc = request.IsNpc,<br/>  HasCompletedOnboarding =<br/>    request.HasCompletedOnboarding,<br/>  Pronouns = request.Pronouns,<br/>  Bio = request.Bio,<br/>  CreatedAt = Now,<br/>  UpdatedAt = Now,<br/>  AvatarMediaId = request.SelectedAvatarMediaId,<br/>  SelectedAvatarMediaId =<br/>    request.SelectedAvatarMediaId<br/>}
    
    Note over UseCase: Step 5: Auto-Update Age Group
    alt DateOfBirth Provided
        UseCase->>UseCase: profile.UpdateAgeGroupFromBirthDate()
        UseCase->>UseCase: Calculate age from DateOfBirth<br/>Update AgeGroupName based on age
    end
    
    Note over UseCase: Step 6: Persist Profile
    UseCase->>Repo: AddAsync(profile)
    Repo->>DB: Add entity to DbSet
    UseCase->>UoW: SaveChangesAsync()
    UoW->>DB: Commit transaction
    DB-->>UoW: Success
    UoW-->>UseCase: Success
    Repo-->>UseCase: UserProfile (with ID)
    
    UseCase-->>Service: UserProfile
    Service-->>Controller: UserProfile
    Controller-->>Client: 201 Created<br/>(UserProfile)
```

## Validation

### 1. Profile Uniqueness

- Checks if profile with same ID already exists
- Throws `ArgumentException` if duplicate

### 2. Fantasy Themes Validation

- Each theme must parse to valid `FantasyTheme` domain object
- Invalid themes cause `ArgumentException` with list of invalid themes

### 3. Age Group Validation

- Age group must be in `AgeGroupConstants.AllAgeGroups`
- Valid values: `school`, `preteens`, `teens`
- Invalid age group causes `ArgumentException`

### 4. Age Group Auto-Update

- If `DateOfBirth` is provided, age group is automatically calculated
- Uses `UpdateAgeGroupFromBirthDate()` domain method
- Overrides explicitly set age group if date of birth provided

## Profile Properties

### Required Properties

- `Id`: Profile identifier (from request)
- `Name`: Profile name
- `AccountId`: Parent account ID
- `AgeGroupName`: Age group (validated)

### Optional Properties

- `PreferredFantasyThemes`: List of fantasy themes (validated)
- `DateOfBirth`: Date of birth (triggers age group update)
- `IsGuest`: Guest profile flag
- `IsNpc`: NPC profile flag
- `HasCompletedOnboarding`: Onboarding completion flag
- `Pronouns`: User pronouns
- `Bio`: Profile biography
- `SelectedAvatarMediaId`: Avatar media ID

### Auto-Generated Properties

- `CreatedAt`: Set to current UTC time
- `UpdatedAt`: Set to current UTC time

## Error Handling

- **Profile Exists**: Returns `ArgumentException` with profile name
- **Invalid Fantasy Themes**: Returns `ArgumentException` with invalid themes list
- **Invalid Age Group**: Returns `ArgumentException` with valid options
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Update User Profile Use Case](./update-user-profile.md)
- [User Profile Domain Model](../../domain/models/user-profile.md)
- [Fantasy Themes](../../domain/models/fantasy-theme.md)
