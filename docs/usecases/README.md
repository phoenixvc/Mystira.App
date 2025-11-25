# Use Cases Documentation

This directory contains comprehensive documentation for all use cases in the Mystira application, organized by domain area.

## Structure

Each domain area has its own subdirectory with:

- **README.md** - Index and overview of use cases in that domain
- Individual use case documentation files with sequence diagrams

## Domain Areas

### [Scenarios](./scenarios/README.md)

Use cases for managing game scenarios (stories):

- Create Scenario
- Get Scenarios (with filtering and pagination)
- Update Scenario
- Delete Scenario
- Validate Scenario

### [Game Sessions](./gamesessions/README.md)

Use cases for managing active game sessions:

- Create Game Session
- Resume Game Session (from active adventures)
- Make Choice
- Progress Scene
- Assign Players to Characters
- End Game Session

### [User Profiles](./userprofiles/README.md)

Use cases for managing user profiles:

- Create User Profile
- Get User Profile
- Update User Profile
- Delete User Profile

### [Badges](./badges/README.md)

Use cases for badge and achievement management:

- Award Badge (via services)
- Get User Badges (via services)
- Badge Configuration Management (via services)

### [Accounts](./accounts/README.md)

Use cases for account management:

- Account operations (via services)
- Subscription management (via services)

### [Characters](./characters/README.md)

Use cases for character map management:

- Character map operations (via services)

### [Media](./media/README.md)

Use cases for media asset management:

- Media upload and management (via services)

### [Avatars](./avatars/README.md)

Use cases for avatar configuration:

- Avatar configuration management (via services)

### [Content Bundles](./content-bundles/README.md)

Use cases for content bundle management:

- Content bundle operations (via services)

## Architecture Flow

All use cases follow the hexagonal architecture pattern:

``` text
Controller → Service → Use Case → Repository → Database
                ↓
            Domain Models
```

Sequence diagrams in each use case document show the complete flow across all layers.

## Parser Integration

Scenario-related use cases integrate with parsers in `Application.Parsers`:

- `ScenarioParser` - Main scenario parsing
- `SceneParser` - Scene parsing
- `CharacterParser` - Character parsing
- `CharacterMetadataParser` - Character metadata parsing
- `BranchParser` - Branch/choice parsing
- `EchoLogParser` - Echo log parsing
- `CompassChangeParser` - Compass change parsing
- `EchoRevealParser` - Echo reveal parsing
- `MediaReferencesParser` - Media references parsing

All parser elements are fully implemented across the entire stack.
