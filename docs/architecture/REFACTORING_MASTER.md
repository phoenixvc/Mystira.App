# Refactoring Master Document

> **📋 Architectural Rules**: See [ARCHITECTURAL_RULES.md](ARCHITECTURAL_RULES.md) for strict enforcement guidelines

This document serves as the index and navigation hub for all architectural refactoring documentation.

## Quick Links

### ⚠️ Essential Reading

- **[Architectural Rules](./ARCHITECTURAL_RULES.md)** - **STRICT ENFORCEMENT GUIDELINES** - Read this first!
- **[Refactoring Status](./REFACTORING_STATUS.md)** - Complete status of all refactoring phases and migrations

### Architecture Patterns

- [Hexagonal Architecture](./patterns/HEXAGONAL_ARCHITECTURE.md) - Architecture overview and principles
- [Repository Pattern](./patterns/REPOSITORY_PATTERN.md) - Repository pattern implementation
- [Unit of Work Pattern](./patterns/UNIT_OF_WORK_PATTERN.md) - Unit of Work pattern details
- [Future Patterns](./patterns/FUTURE_PATTERNS.md) - Planned architectural patterns

### Guides & Classifications

- [API Endpoint Classification](./API_ENDPOINT_CLASSIFICATION.md) - `/api` vs `/adminapi` routing guide

## Current Status Summary

**Current Phase**: Phase 4 - Large File Refactoring ✅

**Completed**:

- ✅ Phase 1: Repository Implementation
- ✅ Phase 2: DTOs Migration
- ✅ Phase 3: Application Layer (70 use cases implemented)
- ✅ Phase 4: Large File Refactoring (ApiClient, MediaApiService, ScenarioRequestCreator, MediaAsset migration)

**In Progress**:

- ⏳ Use case integration into services and controllers
- ⏳ Media use cases implementation (MediaAsset migration complete)

**Pending**:

- ⏳ Phase 5: TypeScript Migration
- ⏳ Phase 6: Cleanup & Documentation
- ⏳ Phase 7: Integration & Testing

For detailed status, see [REFACTORING_STATUS.md](./REFACTORING_STATUS.md).

## Documentation Structure

All architectural documentation is organized in `docs/architecture/`:

### Core Documentation

- **`ARCHITECTURAL_RULES.md`** - ⚠️ **STRICT ENFORCEMENT GUIDELINES** - Architectural rules and principles
- **`REFACTORING_STATUS.md`** - Complete status of all refactoring phases, migrations, and implementations
- **`REFACTORING_MASTER.md`** - This file (index)

### Patterns

- `patterns/HEXAGONAL_ARCHITECTURE.md` - Hexagonal architecture overview
- `patterns/REPOSITORY_PATTERN.md` - Repository pattern details
- `patterns/UNIT_OF_WORK_PATTERN.md` - Unit of Work pattern details
- `patterns/FUTURE_PATTERNS.md` - Planned architectural patterns

### Guides

- `API_ENDPOINT_CLASSIFICATION.md` - API endpoint routing classification
