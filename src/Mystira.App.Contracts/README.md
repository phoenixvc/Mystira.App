# Mystira.App.Contracts

Data Transfer Objects (DTOs) and API contracts for communication between layers. This project serves as the **contract boundary** between the API layer and Application/Domain layers, enabling independent evolution of API and domain models.

## Role in Hexagonal Architecture

**Layer**: **Presentation Contract (DTO Layer)**

The Contracts layer sits at the boundary between presentation and application:
- **Decouples** API shape from domain models
- **Enables** API versioning without domain changes
- **Prepares** for CQRS with separate Request/Response models
- **Facilitates** serialization without polluting domain

**Dependency Flow**:
```
API Layer → Contracts (for DTOs)
Application Layer → Contracts (for use case inputs/outputs)
Domain Layer ← NOT referenced (important!)
```

**Key Principles**:
- ✅ **API Boundary** - Defines external API shape
- ✅ **No Dependencies** - Should have minimal/no dependencies
- ✅ **Serialization-Friendly** - JSON, XML ready
- ✅ **Validation Attributes** - DataAnnotations for API validation

## Project Structure

```
Mystira.App.Contracts/
├── Requests/
│   ├── Accounts/
│   │   ├── CreateAccountRequest.cs
│   │   └── UpdateAccountRequest.cs
│   ├── Auth/
│   │   ├── PasswordlessSigninRequest.cs
│   │   ├── PasswordlessSignupRequest.cs
│   │   └── RefreshTokenRequest.cs
│   ├── Badges/
│   │   ├── AwardBadgeRequest.cs
│   │   ├── CreateBadgeConfigurationRequest.cs
│   │   └── UpdateBadgeConfigurationRequest.cs
│   ├── CharacterMaps/
│   │   ├── CreateCharacterMapRequest.cs
│   │   └── UpdateCharacterMapRequest.cs
│   ├── Contributors/
│   │   ├── RegisterIpAssetRequest.cs
│   │   └── SetContributorsRequest.cs
│   ├── GameSessions/
│   │   ├── StartGameSessionRequest.cs
│   │   ├── MakeChoiceRequest.cs
│   │   └── ProgressSceneRequest.cs
│   └── Media/
│       └── ClientStatusRequest.cs
├── Responses/
│   └── (Response DTOs)
└── Models/
    └── (Shared DTOs)
```

## Purpose

This project decouples API contracts from domain models, allowing:
- **Independent Versioning** - Change API without changing domain
- **Clear Separation** - Presentation vs. business logic
- **Easier Evolution** - API v1, v2, v3 without domain impact
- **CQRS Preparation** - Commands (Requests) and Queries (Responses) separated

## Usage

### When to Use DTOs (Contracts)

**Use DTOs for**:
- ✅ API request/response models
- ✅ Data transfer between API and Application
- ✅ Serialization/deserialization
- ✅ External API contracts
- ✅ Client-facing data shapes

**Example**:
```csharp
// API endpoint receives DTO
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
{
    // Map DTO to domain or pass to use case
    var scenario = await _createScenarioUseCase.ExecuteAsync(request);
    return Ok(scenario);
}
```

### When to Use Domain Models

**Use Domain Models for**:
- ✅ Business logic
- ✅ Domain validation
- ✅ Core entities
- ✅ Internal processing
- ✅ Database persistence

**Example**:
```csharp
// Inside use case, work with domain
public class CreateScenarioUseCase
{
    public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
    {
        // Create domain entity
        var scenario = new Scenario
        {
            Title = request.Title,
            AgeGroup = AgeGroup.Parse(request.AgeGroup),
            // Business logic here
        };

        await _repository.AddAsync(scenario);
        return scenario;
    }
}
```

## 🔍 Architectural Analysis

### Current State Assessment

**File Count**: ~60 Request/Response files
**Dependencies**: None (good ✅)
**Target Framework**: Likely netstandard2.1 or net9.0

### ✅ What's Working Well

1. **Clean Structure** - Organized by feature (Accounts/, Auth/, Badges/, etc.)
2. **Request/Response Separation** - CQRS-ready
3. **No Dependencies** - Pure DTOs
4. **Feature-Based Organization** - Easy to navigate

### 🎯 CQRS Readiness

**Status**: **Excellent preparation for CQRS!**

Current structure already separates:
- **Commands** (Requests/) - Create, Update, Delete operations
- **Queries** (Responses/) - Read operations

**CQRS Migration Path**:
```
Current:
Requests/ → Commands/
Responses/ → Queries/

Future CQRS:
Commands/ → Command handlers in Application
Queries/ → Query handlers in Application
```

### ⚠️ Potential Issues (Minor)

#### 1. **Possible Over-Coupling to Application** (LOW)
**Issue**: Application layer depends on Contracts

**Impact**:
- ⚠️ Changes to API contracts might affect application layer
- ⚠️ Not ideal for pure Clean Architecture

**Recommendation** (Optional):
- Consider if Application should define its own internal DTOs
- Map at API boundary: Contracts DTO → Application DTO → Domain
- Current approach is acceptable for most scenarios

#### 2. **Missing Response Models** (INFO)
**Location**: `Responses/` folder seems sparse

**Observation**:
- Most APIs might be returning domain models directly
- Should have specific response DTOs for each endpoint

**Recommendation**:
- Add Response DTOs (e.g., `ScenarioResponse`, `GameSessionResponse`)
- Map domain → response DTO at API boundary
- Prevents exposing internal domain structure

## 📋 Refactoring TODO

### 🟢 Medium Priority

- [ ] **Add Response DTOs**
  - Create `Responses/Scenarios/ScenarioResponse.cs`
  - Create `Responses/GameSessions/GameSessionResponse.cs`
  - Create response DTOs for all entities
  - Map domain models → response DTOs in API

- [ ] **Add validation attributes**
  - Add `[Required]`, `[MaxLength]`, etc. to Request DTOs
  - Enables model validation at API boundary
  - Example:
    ```csharp
    public class CreateScenarioRequest
    {
        [Required]
        [MaxLength(200)]
        public string Title { get; set; }

        [Required]
        public string AgeGroup { get; set; }
    }
    ```

### 🔵 Low Priority (Future CQRS)

- [ ] **Rename for CQRS**
  - `Requests/` → `Commands/`
  - `Responses/` → `Queries/` or `QueryResults/`
  - Align with CQRS terminology

- [ ] **Add MediatR integration**
  - Install MediatR package
  - Commands implement `IRequest<TResponse>`
  - Queries implement `IRequest<TResult>`

## 💡 Recommendations

### Immediate Actions
1. **Add validation attributes** - Protect API endpoints
2. **Create response DTOs** - Don't expose domain directly
3. **Document DTO mapping strategy** - Team alignment

### Short-term
1. **Response DTOs for all entities** - Complete the contract
2. **Validation rules** - Centralized validation
3. **API versioning prep** - v1, v2 folder structure

### Long-term (CQRS)
1. **MediatR integration** - Commands and queries as messages
2. **Separate read/write models** - Different DTOs for reads vs writes
3. **Event-driven updates** - Domain events → integration events

## 📊 SWOT Analysis

### Strengths 💪
- ✅ **CQRS-Ready**: Request/Response separation
- ✅ **Clean Structure**: Feature-based organization
- ✅ **No Dependencies**: Pure DTOs
- ✅ **Decouples API from Domain**: Independent evolution
- ✅ **Serialization-Friendly**: JSON ready
- ✅ **Well-Organized**: Easy to navigate

### Weaknesses ⚠️
- ⚠️ **Missing Response DTOs**: May be exposing domain directly
- ⚠️ **Limited Validation**: Need DataAnnotations on requests
- ⚠️ **Application Dependency**: Application depends on Contracts (minor issue)

### Opportunities 🚀
- 📈 **CQRS Migration**: Already structured for it
- 📈 **API Versioning**: Easy to add v1/, v2/ folders
- 📈 **MediatR Integration**: Commands/queries as messages
- 📈 **Separate Read/Write Models**: Optimize for each use case
- 📈 **GraphQL Schema**: Generate from DTOs
- 📈 **Auto-Documentation**: OpenAPI from DTOs
- 📈 **Client Generation**: TypeScript, C# clients from DTOs

### Threats 🔒
- ⚡ **DTO Proliferation**: Too many DTOs can be burdensome
- ⚡ **Mapping Overhead**: Extra mapping layer adds code
- ⚡ **Sync Burden**: Keep DTOs in sync with domain changes
- ⚡ **Over-Engineering**: Might be overkill for simple APIs

### Risk Mitigation
1. **AutoMapper**: Automate DTO ↔ Domain mapping
2. **Shared Base Classes**: Reduce DTO duplication
3. **Code Generation**: Generate DTOs from domain (tools)
4. **Documentation**: Clear mapping strategy for team

## Design Patterns

### DTO Pattern
DTOs are pure data carriers with no behavior:
```csharp
public class CreateScenarioRequest
{
    public string Title { get; set; }
    public string AgeGroup { get; set; }
    public List<SceneRequest> Scenes { get; set; }
}
```

### Request/Response Pattern
Separate DTOs for input and output:
- **Request**: Data coming INTO the API
- **Response**: Data going OUT of the API

### Command/Query Separation (CQRS prep)
- **Commands** (Requests/): Mutate state (Create, Update, Delete)
- **Queries** (Responses/): Read state (Get, List)

## Integration with Other Layers

### API Layer
```csharp
// API receives DTO
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateScenarioRequest request)
{
    var result = await _useCase.ExecuteAsync(request);
    return Ok(result);  // Return DTO or domain?
}
```

### Application Layer
```csharp
// Use case accepts DTO, works with domain
public class CreateScenarioUseCase
{
    public async Task<Scenario> ExecuteAsync(CreateScenarioRequest request)
    {
        var scenario = MapTodom(request);
        await _repository.AddAsync(scenario);
        return scenario;
    }
}
```

## Best Practices

1. **DTOs Should Be Dumb** - No logic, just data
2. **Validation at API Boundary** - Use DataAnnotations
3. **Map at Boundaries** - API maps DTO ↔ Domain
4. **Version DTOs** - v1, v2 folders for API versions
5. **Document Changes** - Breaking vs non-breaking DTO changes

## Related Documentation

- **[API](../Mystira.App.Api/README.md)** - Uses DTOs for request/response
- **[Application](../Mystira.App.Application/README.md)** - Depends on Contracts
- **[Domain](../Mystira.App.Domain/README.md)** - Independent of contracts

## License

Copyright (c) 2025 Mystira. All rights reserved.

