# OnboardingStep Domain Model

## Overview

The `OnboardingStep` domain model represents individual steps in the user onboarding process. Onboarding guides new users through profile creation, tutorial scenarios, and game mechanics.

## Class Definition

**Namespace**: `Mystira.App.Domain.Models`

**Location**: `src/Mystira.App.Domain/Models/OnboardingStep.cs`

## Properties

| Property      | Type                 | Description                      |
| ------------- | -------------------- | -------------------------------- |
| `StepNumber`  | `int`                | Sequential step number           |
| `Title`       | `string`             | Step title                       |
| `Description` | `string`             | Step description/instructions    |
| `ImagePath`   | `string`             | Image path for step illustration |
| `Type`        | `OnboardingStepType` | Type of onboarding step          |

## OnboardingStepType Enum

Enumeration of onboarding step types:

- `Welcome` - Initial welcome screen
- `ProfileCreation` - Profile creation step
- `TutorialScenarioGeneration` - Tutorial scenario generation
- `TutorialContentDisplay` - Tutorial content display
- `TutorialDiceMechanic` - Tutorial for dice/roll mechanics
- `Complete` - Onboarding completion

## Onboarding Flow

1. **Welcome** - Introduces user to Mystira
2. **ProfileCreation** - Guides user through creating a profile
3. **TutorialScenarioGeneration** - Explains scenario generation
4. **TutorialContentDisplay** - Shows how content is displayed
5. **TutorialDiceMechanic** - Explains dice/roll mechanics
6. **Complete** - Marks onboarding as complete

## Relationships

- `OnboardingStep` → `UserProfile` (via `HasCompletedOnboarding`)

## Persistence

- Typically stored as configuration data
- Can be stored in Cosmos DB or configuration files
- Step order is determined by `StepNumber`

## Related Documentation

- [UserProfile Domain Model](./user-profile.md)
- [Use Cases Documentation](../usecases/README.md)
