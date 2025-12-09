# BadgeConfiguration Domain Model

## Overview

The `BadgeConfiguration` domain model defines badge definitions that specify when badges should be awarded. Badge configurations are tied to compass axes and define thresholds that trigger badge awards.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/BadgeConfiguration.cs`

## Properties

| Property    | Type       | Description                                           |
| ----------- | ---------- | ----------------------------------------------------- |
| `Id`        | `string`   | Unique identifier                                     |
| `Name`      | `string`   | Badge name                                            |
| `Message`   | `string`   | Badge message/description                             |
| `Axis`      | `string`   | Compass axis (from MasterLists.CompassAxes)           |
| `Threshold` | `float`    | Compass value threshold to trigger badge              |
| `ImageId`   | `string`   | Media path (e.g., "media/images/badge_honesty_1.jpg") |
| `CreatedAt` | `DateTime` | Creation timestamp                                    |
| `UpdatedAt` | `DateTime` | Last update timestamp                                 |

## Related Domain Models

### BadgeConfigurationYaml

YAML structure for badge configuration export/import.

**Properties**:

- `Badges` - `List<BadgeConfigurationYamlEntry>`

### BadgeConfigurationYamlEntry

YAML entry structure for a single badge configuration.

**Properties**:

| Property    | Type     | Description                                          |
| ----------- | -------- | ---------------------------------------------------- |
| `Id`        | `string` | Badge configuration ID                               |
| `Name`      | `string` | Badge name                                           |
| `Message`   | `string` | Badge message                                        |
| `Axis`      | `string` | Compass axis                                         |
| `Threshold` | `float`  | Threshold value                                      |
| `ImageId`   | `string` | Image path (using underscore for YAML compatibility) |

## Badge Awarding Logic

Badges are awarded when:

1. Compass value reaches or exceeds the `Threshold` value
2. The compass `Axis` matches the badge configuration axis
3. The badge has not already been earned by the user profile

## Relationships

- `BadgeConfiguration` → `UserBadge` (via `BadgeConfigurationId`)
- `BadgeConfiguration` → `CoreAxis` (via `Axis`)

## Use Cases

**Current Status**: ❌ No use cases (all operations in services)

**Use Cases** (Should be implemented):

- ❌ `GetBadgeConfigurationsUseCase` - Get all badge configurations
- ❌ `GetBadgeConfigurationUseCase` - Get configuration by ID
- ❌ `GetBadgeConfigurationsByAxisUseCase` - Get configurations for axis
- ❌ `CreateBadgeConfigurationUseCase` - Create new configuration
- ❌ `UpdateBadgeConfigurationUseCase` - Update configuration
- ❌ `DeleteBadgeConfigurationUseCase` - Delete configuration
- ❌ `ImportBadgeConfigurationUseCase` - Import from YAML
- ❌ `ExportBadgeConfigurationUseCase` - Export to YAML

**Current Implementation**: `BadgeConfigurationApiService` (should be refactored)

**Recommendation**: Create `Application.UseCases.BadgeConfigurations` directory

## YAML Import/Export

Badge configurations support YAML-based import and export:

- **Export**: Converts badge configurations to YAML format
- **Import**: Creates badge configurations from YAML files
- **Structure**: Uses `BadgeConfigurationYaml` and `BadgeConfigurationYamlEntry` classes

## Persistence

- Stored in Cosmos DB via `IBadgeConfigurationRepository`
- Managed through `UnitOfWork` pattern
- Indexed by `Id` and `Axis`
- Can be queried by axis for efficient badge checking

## Related Documentation

- [UserBadge Domain Model](./user-badge.md)
- [CoreAxis Domain Model](./core-axis.md)
- [Compass Domain Models](./compass.md)
- [Use Cases Documentation](../usecases/README.md)

