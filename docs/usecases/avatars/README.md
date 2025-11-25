# Avatar Use Cases

This directory contains documentation for avatar configuration-related operations.

## Overview

Avatar configurations define which avatar images are available for each age group. Avatar operations are primarily handled through services.

## Use Cases

Avatar operations are currently handled through services:

- Avatar configuration management (via services)
- Avatar assignment to user profiles (via user profile updates)

## Related Components

- **Domain Models**: `AvatarConfiguration`, `AvatarConfigurationFile`
- **Services**: `AvatarApiService`
- **Storage**: Azure Blob Storage (for avatar images)
