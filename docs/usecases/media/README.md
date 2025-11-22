# Media Use Cases

This directory contains documentation for media asset-related operations.

## Overview

Media assets represent images, audio, and video files used in scenarios and throughout the application. Media operations are primarily handled through services.

## Use Cases

Media operations are currently handled through services:

- Media upload and storage (via Azure Blob Storage)
- Media metadata management (via services)
- Media retrieval and serving (via services)

## Related Components

- **Domain Models**: `MediaReferences`, `MediaAsset` (if exists)
- **Services**: `MediaApiService`, `MediaMetadataService`
- **Storage**: Azure Blob Storage
