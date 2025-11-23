# Mystira.App.Domain

The core domain layer containing business entities, domain logic, and value objects. This project represents the heart of the hexagonal architecture and has no dependencies on external frameworks or infrastructure concerns.

## Role in Hexagonal Architecture

**Layer**: **Domain (Core/Center)**

The Domain layer is the **innermost circle** of the hexagonal architecture, containing:
- Pure business logic independent of frameworks and infrastructure
- Domain models that represent the core business concepts
- Business rules and invariants
- Domain events and value objects

**Key Principles**:
- ✅ **No external dependencies** - Only references standard libraries (System.Text.Json, YamlDotNet)
- ✅ **Framework-agnostic** - Targets `netstandard2.1` for maximum portability
- ✅ **Self-contained** - All business logic lives here
- ✅ **Dependency Inversion** - Infrastructure and application layers depend on this, not vice versa

## Project Structure

```
Mystira.App.Domain/
├── Models/
│   ├── Account.cs                    # User account entity
│   ├── AvatarConfiguration.cs        # Avatar customization
│   ├── BadgeConfiguration.cs         # Achievement badge definitions
│   ├── BadgeThresholds.cs            # Badge earning thresholds
│   ├── Character.cs                  # Character entity
│   ├── CharacterMap.cs               # Character-to-media mappings
│   ├── CompassTracking.cs            # Moral compass tracking
│   ├── ContentBundle.cs              # Bundled scenario content
│   ├── CoreAxis.cs                   # Moral compass axes
│   ├── EchoLog.cs                    # Moral choice logging
│   ├── GameSession.cs                # Game session entity
│   ├── MediaAsset.cs                 # Media file metadata
│   ├── OnboardingStep.cs             # User onboarding workflow
│   ├── Scenario.cs                   # Interactive story scenario
│   ├── Scene.cs                      # Scenario scene
│   ├── StoryProtocolMetadata.cs      # Blockchain IP metadata
│   ├── UserBadge.cs                  # User-earned badges
│   ├── UserProfile.cs                # User profile entity
│   ├── YamlScenario.cs               # YAML-based scenario
│   └── ...
├── Data/
│   ├── archetypes.yml                # Character archetype definitions
│   ├── echo-types.yml                # Moral echo type definitions
│   └── fantasy-themes.yml            # Fantasy theme catalog
└── Mystira.App.Domain.csproj
```

## Core Domain Entities

### Account
Represents user authentication and authorization.
- DM (Dungeon Master) accounts for content creators
- COPPA-compliant (no child accounts with PII)

### Scenario
The central domain entity representing an interactive D&D-style story:
- **Scenes**: Individual story moments with narrative text
- **Choices**: Player decisions that branch the narrative
- **Echo Logs**: Moral implications of choices
- **Compass Changes**: How choices affect the moral compass
- **Age Groups**: Content appropriateness (Ages 4-6, 7-9, 10-12)

### GameSession
Tracks a player's journey through a scenario:
- Current scene position
- Choice history
- Compass tracking (4 moral axes)
- Echo reveals (moral feedback)
- Session state (Active, Paused, Completed)

### UserProfile
Player profile with preferences and progress:
- Display name and avatar
- Fantasy theme preference
- Age group targeting
- Onboarding status
- Earned badges

### BadgeConfiguration
Achievement definitions based on moral compass alignment:
- Axis-based badges (e.g., "Champion of Justice")
- Threshold requirements (min/max values)
- Badge metadata (name, description, icon)

### CompassTracking
Real-time moral compass value tracking:
- **Four Core Axes**:
  - Justice ↔ Mercy
  - Truth ↔ Harmony
  - Courage ↔ Caution
  - Independence ↔ Cooperation
- Value range: -100 to +100
- Historical change tracking

### StoryProtocolMetadata
Blockchain IP registration for content creators:
- IP Asset ID (on-chain identifier)
- Contributor royalty splits
- Registration transaction hash
- Revenue sharing configuration

## Value Objects and Enums

### StringEnum Pattern
Type-safe string enums with predefined values:
```csharp
public class FantasyTheme : StringEnum
{
    public static readonly FantasyTheme Dragons = new("dragons");
    public static readonly FantasyTheme Unicorns = new("unicorns");
    // ...
}
```

### Core Enumerations
- **AgeGroup**: Ages4to6, Ages7to9, Ages10to12
- **SessionState**: Active, Paused, Completed
- **EchoType**: Predefined moral echo categories
- **Archetype**: Character personality types
- **CoreAxis**: Moral compass axis definitions
- **FantasyTheme**: Visual theme preferences

## Domain Logic Examples

### Scenario Validation
Scenarios enforce business rules:
- Maximum 4 character archetypes
- Maximum 4 compass axes
- Age-appropriate content validation
- Echo/compass value ranges

### Compass Tracking
Automatic calculation of moral compass values:
```csharp
public void ApplyCompassChange(CoreAxis axis, int value)
{
    var currentValue = Axes.GetValueOrDefault(axis.Name, 0);
    var newValue = Math.Clamp(currentValue + value, -100, 100);
    Axes[axis.Name] = newValue;
}
```

### Badge Earning Logic
Badge eligibility based on compass thresholds:
```csharp
public bool IsEligibleForBadge(CompassTracking tracking)
{
    var axisValue = tracking.Axes.GetValueOrDefault(Axis, 0);
    return axisValue >= MinValue && axisValue <= MaxValue;
}
```

## Data Definitions (YAML)

The Domain includes master data definitions loaded from YAML:

### archetypes.yml
Defines character personality archetypes:
- Name, description
- Base compass values
- Personality traits

### echo-types.yml
Master list of moral echo categories:
- Echo type names
- Descriptions
- Associated moral lessons

### fantasy-themes.yml
Visual theme catalog:
- Theme names
- Asset categories
- UI styling preferences

## Domain Events (Future)

The domain is designed to support domain events for:
- Scenario completion
- Badge earning
- Compass threshold crossing
- Achievement unlocking

## Validation Rules

Domain entities enforce invariants:
- **Account**: Valid email, unique username
- **Scenario**: Required title, valid age group, max limits
- **GameSession**: Valid scenario reference, non-empty choices
- **CompassTracking**: Values within -100 to +100 range
- **BadgeConfiguration**: Valid axis, threshold ranges

## Technology Stack

- **Target Framework**: `netstandard2.1` (maximum compatibility)
- **JSON Serialization**: `System.Text.Json`
- **YAML Parsing**: `YamlDotNet` for master data
- **No Dependencies**: On infrastructure, frameworks, or ORMs

## Usage in Other Layers

### Application Layer
```csharp
using Mystira.App.Domain.Models;

public class CreateScenarioUseCase
{
    public async Task<Scenario> ExecuteAsync(ScenarioRequest request)
    {
        var scenario = new Scenario
        {
            Title = request.Title,
            AgeGroup = request.AgeGroup,
            // ... domain logic
        };

        return scenario;
    }
}
```

### Infrastructure Layer
```csharp
using Mystira.App.Domain.Models;

public class ScenarioRepository : IRepository<Scenario>
{
    // Maps domain entities to/from database
}
```

### API Layer
```csharp
using Mystira.App.Domain.Models;

[ApiController]
public class ScenariosController : ControllerBase
{
    // Returns domain entities as DTOs
}
```

## Design Patterns

### Repository Pattern (Interface)
Domain defines repository contracts:
```csharp
public interface IRepository<T>
{
    Task<T?> GetByIdAsync(string id);
    Task<IEnumerable<T>> GetAllAsync();
    // ...
}
```

### Unit of Work (Interface)
Domain defines transaction boundaries:
```csharp
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync();
    // ...
}
```

### Domain-Driven Design
- **Entities**: Objects with identity (Account, Scenario, GameSession)
- **Value Objects**: Objects defined by values (CoreAxis, EchoType)
- **Aggregates**: Scenario is the root aggregate with Scenes and Choices
- **Domain Services**: Complex logic that doesn't fit in entities

## Best Practices

1. **Keep It Pure**: No infrastructure concerns in domain models
2. **Validate Early**: Enforce business rules in domain entities
3. **Encapsulation**: Protect domain invariants with private setters
4. **Immutability**: Value objects should be immutable when possible
5. **Rich Domain Model**: Business logic lives in domain, not services
6. **Ubiquitous Language**: Model names match business terminology

## Testing Domain Logic

Domain entities should be unit tested without infrastructure:

```csharp
[Fact]
public void CompassTracking_ApplyChange_ShouldClampValues()
{
    var tracking = new CompassTracking();
    tracking.ApplyCompassChange(CoreAxis.Justice, 150);

    Assert.Equal(100, tracking.Axes["Justice"]); // Clamped to max
}
```

## Future Enhancements

- **Domain Events**: Publish events on entity state changes
- **Specifications**: Reusable query specifications
- **Domain Services**: Extract complex multi-entity logic
- **Aggregate Patterns**: Stronger aggregate boundaries

## Related Documentation

- **[Application Layer](../Mystira.App.Application/README.md)** - Use cases that orchestrate domain logic
- **[Infrastructure.Data](../Mystira.App.Infrastructure.Data/README.md)** - Repository implementations
- **[Contracts](../Mystira.App.Contracts/README.md)** - DTOs that expose domain to APIs

## License

Copyright (c) 2025 Mystira. All rights reserved.
