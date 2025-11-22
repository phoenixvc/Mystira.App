# Get User Profile Use Case

## Overview

The `GetUserProfileUseCase` retrieves a user profile by ID.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.UserProfiles.GetUserProfileUseCase`

**Input**: `string id`

**Output**: `UserProfile?` (domain model, null if not found)

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as UserProfilesController
    participant Service as UserProfileApiService
    participant UseCase as GetUserProfileUseCase
    participant Repo as IUserProfileRepository
    participant DB as CosmosDB

    Client->>Controller: GET /api/userprofiles/{id}
    Controller->>Service: GetProfileAsync(id)
    
    Service->>UseCase: ExecuteAsync(id)
    
    UseCase->>Repo: GetByIdAsync(id)
    Repo->>DB: Query profile by ID
    alt Profile Not Found
        DB-->>Repo: null
        Repo-->>UseCase: null
        UseCase-->>Service: null
        Service-->>Controller: NotFound
        Controller-->>Client: 404 Not Found
    else Profile Found
        DB-->>Repo: UserProfile
        Repo-->>UseCase: UserProfile
        UseCase-->>Service: UserProfile
        Service-->>Controller: UserProfile
        Controller-->>Client: 200 OK<br/>(UserProfile)
    end
```

## Behavior

- **Simple Lookup**: Direct ID-based retrieval
- **Null Handling**: Returns `null` if profile not found (not an error)
- **Logging**: Logs debug message if profile not found

## Error Handling

- **Profile Not Found**: Returns `null` (handled as 404 by service)
- **Database Error**: Logs error and rethrows exception

## Related Documentation

- [Create User Profile Use Case](./create-user-profile.md)
- [Update User Profile Use Case](./update-user-profile.md)
- [User Profile Domain Model](../../domain/models/user-profile.md)
