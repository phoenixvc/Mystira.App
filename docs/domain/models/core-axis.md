# CoreAxis Domain Model

## Overview

The `CoreAxis` domain model represents compass core axes using the `StringEnum<T>` pattern. Core axes define the moral/ethical dimensions tracked in scenarios (e.g., honesty, kindness, bravery).

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/CoreAxis.cs`

## Base Class

Extends `StringEnum<CoreAxis>` - See [StringEnum Documentation](./string-enum.md) for base functionality.

## Properties

| Property | Type     | Description                            |
| -------- | -------- | -------------------------------------- |
| `Value`  | `string` | Core axis identifier (from StringEnum) |

## JSON Configuration

Core axes are loaded from `Data/CoreAxiss.json` (note: plural form):

```json
[
  { "Value": "honesty" },
  { "Value": "kindness" },
  { "Value": "bravery" },
  { "Value": "wisdom" },
  { "Value": "curiosity" }
]
```

## Usage

```csharp
// Parse core axis
var axis = CoreAxis.Parse("honesty");

// Check if valid
if (CoreAxis.TryParse("honesty", out var result))
{
    // Use result
}

// Compare (case-insensitive)
if (axis == CoreAxis.Parse("HONESTY"))
{
    // Equal
}

// Get all axes
var allAxes = CoreAxis.ValueMap.Values;
```

## Relationships

- `CoreAxis` → `Scenario` (via `CoreAxes` list)
- `CoreAxis` → `CompassTracking` (via axis name)
- `CoreAxis` → `CompassChange` (via axis name)
- `CoreAxis` → `BadgeConfiguration` (via `Axis`)

## Compass Integration

Core axes are used throughout the compass system:

- **Scenarios** define which axes to track (`Scenario.CoreAxes`)
- **Compass Tracking** stores values per axis (`CompassTracking.Axis`)
- **Compass Changes** modify axis values (`CompassChange.Axis`)
- **Badge Configurations** are tied to axes (`BadgeConfiguration.Axis`)

## JSON Serialization

Uses `StringEnumJsonConverter<CoreAxis>` for JSON serialization:

- Serializes to string value
- Deserializes from string value
- Case-insensitive parsing

## Related Documentation

- [StringEnum Base Class](./string-enum.md)
- [Scenario Domain Model](./scenario.md)
- [Compass Domain Models](./compass.md)
- [BadgeConfiguration Domain Model](./badge-configuration.md)
