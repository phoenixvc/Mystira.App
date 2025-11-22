# FantasyTheme Domain Model

## Overview

The `FantasyTheme` domain model represents fantasy themes using the `StringEnum<T>` pattern. Fantasy themes define the thematic preferences of user profiles and can be used for content filtering and recommendations.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/FantasyTheme.cs`

## Base Class

Extends `StringEnum<FantasyTheme>` - See [StringEnum Documentation](./string-enum.md) for base functionality.

## Properties

| Property      | Type     | Description                                    |
| ------------- | -------- | ---------------------------------------------- |
| `Value`       | `string` | Fantasy theme identifier (from StringEnum)     |

## JSON Configuration

Fantasy themes are loaded from `Data/FantasyThemes.json`:

```json
[
  { "Value": "medieval" },
  { "Value": "modern" },
  { "Value": "sci-fi" },
  { "Value": "steampunk" },
  { "Value": "urban-fantasy" }
]
```

## Usage

```csharp
// Parse fantasy theme
var theme = FantasyTheme.Parse("medieval");

// Check if valid
if (FantasyTheme.TryParse("medieval", out var result))
{
    // Use result
}

// Compare (case-insensitive)
if (theme == FantasyTheme.Parse("MEDIEVAL"))
{
    // Equal
}

// Get all themes
var allThemes = FantasyTheme.ValueMap.Values;
```

## Relationships

- `FantasyTheme` → `UserProfile` (via `PreferredFantasyThemes` list)

## User Profile Integration

Fantasy themes are stored as preferences in user profiles:

- **UserProfile** maintains a list of preferred themes (`UserProfile.PreferredFantasyThemes`)
- Themes can be used for:
  - Content filtering (show scenarios matching preferred themes)
  - Recommendations (suggest content based on theme preferences)
  - Personalization (customize experience based on themes)

## JSON Serialization

Uses standard `StringEnum<T>` serialization (no custom converter):

- Serializes to string value
- Deserializes from string value
- Case-insensitive parsing

## Related Documentation

- [StringEnum Base Class](./string-enum.md)
- [UserProfile Domain Model](./user-profile.md)

