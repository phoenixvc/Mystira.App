# Character Use Cases

This directory contains documentation for character map-related operations.

## Overview

Character maps represent reusable character definitions that can be used across multiple scenarios. Character operations are primarily handled through services.

## Use Cases

Character map operations are currently handled through services:

- Character map CRUD operations (via services)
- Character assignment to game sessions (via `SelectCharacterAsync`)

## Related Components

- **Domain Models**: `CharacterMap`, `CharacterMetadata`
- **Services**: `CharacterMapFileService`
- **Use Cases**: [Assign Player to Character](../gamesessions/assign-character.md)
