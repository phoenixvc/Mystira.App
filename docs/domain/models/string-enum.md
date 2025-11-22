# StringEnum Base Class

## Overview

The `StringEnum<T>` base class provides a type-safe pattern for string-based enumerations. It allows loading enum values from JSON files and provides parsing, comparison, and serialization capabilities.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/StringEnum.cs`

## Generic Type Parameter

- `T` - The derived class type (e.g., `Archetype`, `CoreAxis`)

## Properties

| Property   | Type                             | Description                                   |
| ---------- | -------------------------------- | --------------------------------------------- |
| `Value`    | `string`                         | The string value of the enum                  |
| `ValueMap` | `IReadOnlyDictionary<string, T>` | Static dictionary of all values (lazy-loaded) |

## Methods

### Static Methods

#### `Parse(string? value)`

Parses a string value into the enum type.

**Parameters**:

- `value` - The string value to parse

**Returns**: `T?` - The parsed enum value, or null if not found

#### `TryParse(string? value, out T? result)`

Attempts to parse a string value into the enum type.

**Parameters**:

- `value` - The string value to parse
- `result` - Output parameter for the parsed value

**Returns**: `bool` - True if parsing succeeded, false otherwise

### Instance Methods

#### `ToString()`

Returns the string value of the enum.

**Returns**: `string` - The enum value

#### `Equals(object? obj)`

Compares two enum values for equality (case-insensitive).

**Returns**: `bool` - True if values are equal

#### `GetHashCode()`

Returns hash code based on the enum value.

**Returns**: `int` - Hash code

### Operators

- `==` - Equality comparison (case-insensitive)
- `!=` - Inequality comparison (case-insensitive)

## JSON File Loading

Enum values are loaded from JSON files:

- **File Location**: `Data/{TypeName}s.json`
- **Format**: Array of enum objects
- **Loading**: Lazy-loaded on first access to `ValueMap`
- **Case Sensitivity**: Case-insensitive matching

### Example JSON File

```json
[
  { "Value": "guardian" },
  { "Value": "the listener" },
  { "Value": "trickster" }
]
```

## Derived Classes

The following domain models extend `StringEnum<T>`:

- `Archetype` - Character archetypes
- `CoreAxis` - Compass core axes
- `EchoType` - Echo types
- `FantasyTheme` - Fantasy themes
- `AgeGroup` - Age group classifications (with additional properties)

## Usage Example

```csharp
// Parse a value
var archetype = Archetype.Parse("guardian");

// Check if value exists
if (Archetype.TryParse("guardian", out var result))
{
    // Use result
}

// Compare values (case-insensitive)
if (archetype == Archetype.Parse("GUARDIAN"))
{
    // Values are equal
}

// Get all values
var allArchetypes = Archetype.ValueMap.Values;
```

## JSON Serialization

Derived classes use `StringEnumJsonConverter<T>` for JSON serialization:

- Serializes to string value
- Deserializes from string value
- Case-insensitive parsing

## Related Documentation

- [Archetype Domain Model](./archetype.md)
- [CoreAxis Domain Model](./core-axis.md)
- [EchoType Domain Model](./echo-type.md)
- [FantasyTheme Domain Model](./fantasy-theme.md)
- [AgeGroup](./user-profile.md#agegroup)
