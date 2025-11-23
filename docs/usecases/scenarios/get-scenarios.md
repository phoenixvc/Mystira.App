# Get Scenarios Use Case

## Overview

The `GetScenariosUseCase` retrieves scenarios with advanced filtering, sorting, and pagination capabilities.

## Use Case Details

**Class**: `Mystira.App.Application.UseCases.Scenarios.GetScenariosUseCase`

**Input**: `ScenarioQueryRequest`

**Output**: `ScenarioListResponse`

## Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant Controller as ScenariosController
    participant Service as ScenarioApiService
    participant UseCase as GetScenariosUseCase
    participant Repo as IScenarioRepository
    participant DB as CosmosDB

    Client->>Controller: GET /api/scenarios<br/>?difficulty=Medium&ageGroup=teens&page=1&pageSize=10
    Controller->>Controller: Map query params<br/>to ScenarioQueryRequest
    Controller->>Service: GetScenariosAsync(request)
    
    Service->>UseCase: ExecuteAsync(request)
    
    Note over UseCase: Step 1: Get Queryable
    UseCase->>Repo: GetQueryable()
    Repo-->>UseCase: IQueryable<Scenario>
    
    Note over UseCase: Step 2: Apply Filters
    alt Filter by Difficulty
        UseCase->>UseCase: query.Where(s => s.Difficulty == request.Difficulty)
    end
    alt Filter by SessionLength
        UseCase->>UseCase: query.Where(s => s.SessionLength == request.SessionLength)
    end
    alt Filter by MinimumAge
        UseCase->>UseCase: query.Where(s => s.MinimumAge <= request.MinimumAge)
    end
    alt Filter by AgeGroup
        UseCase->>UseCase: query.Where(s => s.AgeGroup == request.AgeGroup)
    end
    alt Filter by Tags
        UseCase->>UseCase: query.Where(s => Tags.ContainsAny(request.Tags))
    end
    alt Filter by Archetypes
        UseCase->>UseCase: Parse archetypes<br/>query.Where(s => Archetypes.ContainsAny(parsed))
    end
    alt Filter by CoreAxes
        UseCase->>UseCase: Parse core axes<br/>query.Where(s => CoreAxes.ContainsAny(parsed))
    end
    
    Note over UseCase: Step 3: Get Total Count
    UseCase->>DB: CountAsync(query)
    DB-->>UseCase: totalCount
    
    Note over UseCase: Step 4: Apply Pagination & Sorting
    UseCase->>UseCase: query.OrderByDescending(s => s.CreatedAt)<br/>.Skip((page-1) * pageSize)<br/>.Take(pageSize)
    
    Note over UseCase: Step 5: Project to DTO
    UseCase->>UseCase: Select(s => new ScenarioSummary {<br/>  Id, Title, Description, Tags,<br/>  Difficulty, SessionLength,<br/>  Archetypes, MinimumAge,<br/>  AgeGroup, CoreAxes, CreatedAt<br/>})
    
    UseCase->>DB: ToListAsync()
    DB-->>UseCase: List<ScenarioSummary>
    
    Note over UseCase: Step 6: Build Response
    UseCase->>UseCase: new ScenarioListResponse {<br/>  Scenarios, TotalCount,<br/>  Page, PageSize, HasNextPage<br/>}
    
    UseCase-->>Service: ScenarioListResponse
    Service-->>Controller: ScenarioListResponse
    Controller-->>Client: 200 OK<br/>(ScenarioListResponse)
```

## Filtering Options

### Supported Filters

1. **Difficulty**: Exact match (`Easy`, `Medium`, `Hard`)
2. **SessionLength**: Exact match (`Short`, `Medium`, `Long`)
3. **MinimumAge**: Less than or equal (`MinimumAge <= request.MinimumAge`)
4. **AgeGroup**: Exact match (`school`, `preteens`, `teens`)
5. **Tags**: Contains any (`s.Tags.ContainsAny(request.Tags)`)
6. **Archetypes**: Contains any (parsed to `Archetype` domain objects)
7. **CoreAxes**: Contains any (parsed to `CoreAxis` domain objects)

### Filter Combination

All filters are combined with AND logic - scenarios must match all specified filters.

## Pagination

- **Page**: 1-based page number (default: 1)
- **PageSize**: Number of items per page (default: 10)
- **HasNextPage**: Calculated as `(Page * PageSize) < TotalCount`

## Sorting

Scenarios are sorted by `CreatedAt` in descending order (newest first).

## Performance Considerations

- Uses `IQueryable<T>` for database-level filtering (efficient)
- Total count query executed before pagination
- Projection to DTO happens at database level (reduces data transfer)
- Archetype and CoreAxis parsing happens in memory (small dataset)

## Related Documentation

- [Scenario Domain Model](../../domain/models/scenario.md)
- [Scenario Query Request](../../contracts/requests/scenarios.md)
