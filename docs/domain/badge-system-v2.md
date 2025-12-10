# Badge System v2

## Overview

The Badge System v2 is a comprehensive achievement/badge framework that supports age-appropriate progression and tiered achievements across different compass axes. This replaces the legacy `BadgeConfiguration` model with a richer, more flexible system.

## Architecture

### Domain Models

#### Badge
The core badge definition that represents an achievement that can be earned.

**Location**: `src/Mystira.App.Domain/Models/Badge.cs`

**Properties**:
- `Id` (string) - Unique identifier
- `AgeGroupId` (string) - Age group this badge applies to (e.g., "1-2", "3-5")
- `CompassAxisId` (string) - Reference to compass axis
- `Tier` (string) - Badge tier (bronze, silver, gold, platinum, diamond)
- `TierOrder` (int) - Order within the tier
- `Title` (string) - Display title
- `Description` (string) - Achievement description
- `RequiredScore` (float) - Score threshold required to earn
- `ImageId` (string) - Reference to badge image
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp

#### AxisAchievement
Represents achievements along compass axes for specific age groups.

**Location**: `src/Mystira.App.Domain/Models/AxisAchievement.cs`

**Properties**:
- `Id` (string) - Unique identifier
- `AgeGroupId` (string) - Age group identifier
- `CompassAxisId` (string) - Reference to compass axis
- `AxesDirection` (string) - Direction on axis (positive/negative)
- `Description` (string) - Achievement description
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp

#### BadgeImage
Stores badge image data and metadata.

**Location**: `src/Mystira.App.Domain/Models/BadgeImage.cs`

**Properties**:
- `Id` (string) - Unique identifier
- `ImageId` (string) - Reference identifier for the image
- `ContentType` (string) - MIME type (default: image/png)
- `ImageData` (byte[]?) - Binary image data
- `FileSizeBytes` (long) - File size
- `CreatedAt` (DateTime) - Creation timestamp
- `UpdatedAt` (DateTime) - Last update timestamp

#### UserBadge (Updated)
Enhanced to support both legacy and new badge system.

**New Property**:
- `BadgeId` (string?) - Reference to new Badge entity

## Configuration System

### JSON Schema

**Location**: `src/Mystira.App.Domain/Schemas/badge-configuration.schema.json`

The schema enforces structure for age-group badge configurations:
- `Age_Group_Id` - Required age group identifier
- `Axis_Achievements` - Array of axis achievement definitions
- `Badges` - Array of badge definitions with tier, order, and requirements

### Configuration Files

**Location**: `src/Mystira.App.Domain/Data/Badges/`

Per-age-group configuration files:
- `1-2.json` - Toddlers (ages 1-2)
- `3-5.json` - Preschoolers (ages 3-5)
- `6-9.json` - School age (ages 6-9)
- `10-12.json` - Preteens (ages 10-12)
- `13-18.json` - Teens (ages 13-18)

Each file conforms to the JSON schema and defines:
- Axis achievements for the age group
- Tiered badges with progressive difficulty
- Age-appropriate titles and descriptions

## Data Access Layer

### Repository Interfaces

**Location**: `src/Mystira.App.Application/Ports/Data/`

- `IAxisAchievementRepository` - CRUD and queries for axis achievements
- `IBadgeRepository` - CRUD and queries for badges
- `IBadgeImageRepository` - CRUD and queries for badge images

### Repository Implementations

**Location**: `src/Mystira.App.Infrastructure.Data/Repositories/`

- `AxisAchievementRepository` - Implements axis achievement data access
- `BadgeRepository` - Implements badge data access
- `BadgeImageRepository` - Implements badge image data access

All repositories support:
- Standard CRUD operations
- Specification pattern queries
- Age group and compass axis filtering

## Loader Service

### BadgeConfigurationLoaderService

**Location**: `src/Mystira.App.Infrastructure.Data/Services/BadgeConfigurationLoaderService.cs`

Responsible for:
1. Loading the JSON schema
2. Validating configuration files against schema
3. Loading badge configurations from JSON files
4. Seeding database with validated configurations
5. Idempotent seeding (skips if already seeded)

**Features**:
- Schema validation using JsonSchema.Net
- Deterministic ID generation for idempotent seeding
- Error handling and logging
- Configurable file paths

## Database Configuration

### DbContext Updates

**Location**: `src/Mystira.App.Infrastructure.Data/MystiraAppDbContext.cs`

New DbSets:
```csharp
public DbSet<AxisAchievement> AxisAchievements { get; set; }
public DbSet<Badge> Badges { get; set; }
public DbSet<BadgeImage> BadgeImages { get; set; }
```

Entity configurations:
- Cosmos DB container mappings with partition keys
- Unique indexes (in-memory mode):
  - AxisAchievement: (AgeGroupId, CompassAxisId, AxesDirection)
  - Badge: (AgeGroupId, CompassAxisId, TierOrder)
  - BadgeImage: (ImageId)

## Startup Integration

### Service Registration

Both Admin API and Client API register the new repositories:

```csharp
builder.Services.AddScoped<IAxisAchievementRepository, AxisAchievementRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();
builder.Services.AddScoped<IBadgeImageRepository, BadgeImageRepository>();
```

### Seeding (Admin API Only)

The Admin API seeds badge configurations on startup:

```csharp
builder.Services.AddScoped<BadgeConfigurationLoaderService>();

// In startup code:
var badgeLoader = scope.ServiceProvider.GetRequiredService<BadgeConfigurationLoaderService>();
await badgeLoader.LoadAndSeedAsync();
```

Seeding is controlled by configuration settings:
- `InitializeDatabaseOnStartup` - Controls database initialization
- `SeedMasterDataOnStartup` - Controls seeding behavior

## Configuration Settings

### appsettings.json

```json
{
  "InitializeDatabaseOnStartup": true,
  "SeedMasterDataOnStartup": true,
  "BadgeConfiguration": {
    "ConfigPath": "src/Mystira.App.Domain/Data/Badges/",
    "SchemaPath": "src/Mystira.App.Domain/Schemas/badge-configuration.schema.json",
    "MaxImageSizeBytes": 5242880
  }
}
```

### Environment-Specific Settings

- **Development (In-Memory)**: Always seeds data
- **Production (Cosmos DB)**: Controlled by configuration flags
- **Testing**: In-memory database with automatic seeding

## Badge Tiers

The system supports five badge tiers in ascending order:
1. **Bronze** - Entry level achievements
2. **Silver** - Intermediate achievements
3. **Gold** - Advanced achievements
4. **Platinum** - Expert level achievements (ages 6+)
5. **Diamond** - Master level achievements (ages 10+)

Age groups have progressively more tiers:
- Ages 1-2: Bronze, Silver
- Ages 3-5: Bronze, Silver, Gold
- Ages 6-9: Bronze, Silver, Gold, Platinum
- Ages 10-12: Bronze, Silver, Gold, Platinum, Diamond
- Ages 13-18: Bronze, Silver, Gold, Platinum, Diamond

## Badge Progression

Badges are earned based on compass axis scores:
- Each compass axis has multiple badges across tiers
- Higher tiers require higher scores
- Scores are age-appropriate and progressive
- Badge requirements are defined in JSON configuration files

### Example Badge Progression (Curiosity & Initiative)

**Ages 6-9**:
- Bronze (10.0 score): Curious Mind
- Silver (20.0 score): Problem Solver
- Gold (35.0 score): Innovation Champion
- Platinum (50.0 score): Master Inventor

## Migration from Legacy System

The new system coexists with the legacy `BadgeConfiguration`:

### Legacy Support
- `UserBadge.BadgeConfigurationId` - Existing field (kept for compatibility)
- `UserBadge.BadgeId` - New field (nullable for backward compatibility)

### Migration Path
1. New badges use `BadgeId` to reference Badge entity
2. Legacy badges continue to use `BadgeConfigurationId`
3. Systems can query both for complete badge history

## Future Enhancements

### Planned Features
- Badge image upload and management
- Dynamic badge requirement adjustments
- Badge statistics and analytics
- Achievement milestones
- Badge sharing and social features
- Custom badge creation (admin)

### API Endpoints
Future endpoints to expose:
- `GET /api/badges?ageGroup={id}` - Get badges for age group
- `GET /api/badges/{id}` - Get badge details
- `GET /api/axis-achievements?ageGroup={id}` - Get achievements
- `GET /api/user-badges/{profileId}` - Get earned badges

## Related Documentation

- [BadgeConfiguration (Legacy)](./models/badge-configuration.md)
- [UserBadge](./models/user-badge.md)
- [CompassAxis](./models/compass-axis.md)
- [AgeGroup](./models/age-group.md)

## Testing

### Unit Tests
Create tests for:
- BadgeConfigurationLoaderService validation
- Repository implementations
- Schema validation
- ID generation determinism

### Integration Tests
- End-to-end badge earning workflow
- Configuration loading and seeding
- Database persistence

## Dependencies

### NuGet Packages
- `JsonSchema.Net` (v7.2.2) - JSON schema validation
- `Microsoft.EntityFrameworkCore` (v9.0.0) - Data access
- `Microsoft.EntityFrameworkCore.Cosmos` (v9.0.0) - Cosmos DB provider

## Security Considerations

- Configuration files are read-only at runtime
- Schema validation prevents malformed configurations
- Image size limits prevent abuse (5MB default)
- Repository layer enforces access patterns
