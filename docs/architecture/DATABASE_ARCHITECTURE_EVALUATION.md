# Database Architecture Evaluation

**Date**: 2025-12-29
**Context**: Evaluation of WhatsApp architecture discussion vs actual implementation

---

## Executive Summary

The WhatsApp conversation discussed a polyglot database architecture using:
- **Blue**: PostgreSQL (relational, frequently updated data)
- **Yellow**: Cosmos DB (hierarchical/deeply nested, rarely updated)
- **Green**: Redis + Blob Storage (caching + media assets)
- **?**: Unknown (later identified as Elasticsearch, Data Lake, Pinecone)

This document evaluates the discussion against the **actual Mystira.App implementation**.

---

## Current Implementation (Reality Check)

### Technology Stack

| Layer | Technology | Status |
|-------|------------|--------|
| **Primary Database** | Azure Cosmos DB | ✅ Active |
| **Secondary Database** | PostgreSQL | 🔮 Planned (not implemented) |
| **Cache** | Redis | ✅ Active (optional) |
| **Blob Storage** | Azure Blob Storage | ✅ Active |
| **Migration Framework** | Polyglot Persistence | ✅ Ready (Phase: CosmosOnly) |

### Key Finding

> **The system is currently 100% Cosmos DB** - PostgreSQL migration is planned but not yet active.

The codebase has a sophisticated polyglot persistence layer (`PolyglotRepository<T>`) ready for migration, with these phases defined in `MigrationPhase.cs`:

```
CosmosOnly → DualWriteCosmosRead → DualWritePostgresRead → PostgresOnly
     ↑
  CURRENT
```

---

## Entity-by-Entity Analysis

### Discussion vs Implementation

| Entity | WhatsApp Decision | Current Implementation | Evaluation |
|--------|-------------------|------------------------|------------|
| **accounts** | PostgreSQL | Cosmos DB (`/id` partition) | ⚠️ Currently Cosmos, planned for PG |
| **gamesessions** | PostgreSQL | Cosmos DB (`/accountId` partition) | ⚠️ Good partition key choice! |
| **user profiles** | Cosmos (uncertain) | Cosmos DB (`/id` partition) | ✅ Correctly identified |
| **playerscenarioscores** | PostgreSQL | Cosmos DB (`/profileId` partition) | ⚠️ Currently Cosmos, good for PG |
| **avatarconfiguration** | Cosmos/uncertain | Cosmos DB (as `AvatarConfigurationFile`) | 🔄 Simple mapping - could be either |
| **charactermediametadata** | Delete/Cosmos | Cosmos DB (`CharacterMediaMetadataFile`) | ✅ Exists, stored as Cosmos document |
| **mystiraappdbcontext** | Unknown | EF Core DbContext class | ❓ Not an entity - it's infrastructure |

---

## Detailed Container Mapping (26 Cosmos Containers)

### User & Profile Data
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ Accounts               │ /id           │ ✅ YES - Relational │
│ UserProfiles           │ /id           │ ⚠️ Maybe - Split    │
│ UserBadges             │ (embedded)    │ ✅ YES - FK to user │
└─────────────────────────────────────────────────────────────┘
```

### Game Session Data
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ GameSessions           │ /accountId    │ ✅ YES - ACID needed│
│ PlayerScenarioScores   │ /profileId    │ ✅ YES - Relational │
└─────────────────────────────────────────────────────────────┘
```

### Content & Scenario Data
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ Scenarios              │ /id           │ ⚠️ Complex nested   │
│ ContentBundles         │ /id           │ ❌ Keep Cosmos      │
│ CharacterMaps          │ /id           │ ❌ Keep Cosmos      │
└─────────────────────────────────────────────────────────────┘
```

### Media & Metadata
```
┌─────────────────────────────────────────────────────────────┐
│ Container                  │ Partition Key │ PostgreSQL?    │
├────────────────────────────┼───────────────┼────────────────┤
│ MediaAssets                │ /mediaType    │ ❌ Keep Cosmos │
│ MediaMetadataFiles         │ /id           │ ❌ Keep Cosmos │
│ CharacterMediaMetadataFiles│ /id           │ ❌ DELETE?     │
│ CharacterMapFiles          │ /id           │ ❌ Keep Cosmos │
│ AvatarConfigurationFiles   │ /id           │ ⚠️ Simple map  │
└─────────────────────────────────────────────────────────────┘
```

### Configuration (Read-heavy, rarely updated)
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ BadgeConfigurations    │ /id           │ ❌ Keep Cosmos      │
│ CompassAxes            │ /id           │ ❌ Keep Cosmos      │
│ ArchetypeDefinitions   │ /id           │ ❌ Keep Cosmos      │
│ EchoTypeDefinitions    │ /id           │ ❌ Keep Cosmos      │
│ FantasyThemeDefinitions│ /id           │ ❌ Keep Cosmos      │
│ AgeGroupDefinitions    │ /id           │ ❌ Keep Cosmos      │
└─────────────────────────────────────────────────────────────┘
```

### Badge System
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ Badges                 │ /id           │ ❌ Keep Cosmos      │
│ BadgeImages            │ /id           │ ❌ Keep Cosmos      │
│ AxisAchievements       │ /id           │ ❌ Keep Cosmos      │
└─────────────────────────────────────────────────────────────┘
```

### Analytics
```
┌─────────────────────────────────────────────────────────────┐
│ Container              │ Partition Key │ PostgreSQL Candidate│
├────────────────────────┼───────────────┼─────────────────────┤
│ CompassTrackings       │ /axis         │ ⚠️ Analytics use    │
└─────────────────────────────────────────────────────────────┘
```

---

## Evaluation of WhatsApp Discussion

### Correct Assessments ✅

1. **Accounts → PostgreSQL**: Correct reasoning. Accounts require ACID transactions, have referential integrity needs, and are frequently updated.

2. **GameSessions → PostgreSQL**: Correct. Changes every scene, needs ACID for state consistency. The current `/accountId` partition key is excellent for Cosmos but the relational nature suits PostgreSQL.

3. **PlayerScenarioScores → PostgreSQL**: Correct. This is clearly relational data (scores per compass axes per profile per game). The `/profileId` partition is good for Cosmos but joins would benefit from PostgreSQL.

4. **Blob for media**: Correct. Azure Blob Storage is used for all binary assets.

5. **CharacterMediaMetadata → potentially delete/Cosmos**: The codebase has this as `CharacterMediaMetadataFile`. The discussion about unstructured, strangely-queried metadata is accurate for keeping it in Cosmos.

### Incorrect/Uncertain Assessments ⚠️

1. **Redis in "Green" with Blob**:
   - **Issue**: Redis is cache, Blob is storage - they serve different purposes
   - **Reality**: The codebase correctly separates these. Redis is optional caching layer via `CachedRepository.cs`, Blob is for media storage

2. **UserProfiles → Cosmos only**:
   - **Better approach**: Split architecture as discussed - core profile in PostgreSQL, extended preferences in Cosmos
   - **Current**: All in Cosmos with embedded `EarnedBadges`

3. **AvatarConfiguration decision uncertainty**:
   - **Reality**: It's stored as `AvatarConfigurationFiles` with `Dictionary<string, List<string>>` for age group → avatar mappings
   - **Assessment**: Simple lookup table, could be PostgreSQL

4. **"mystiraappdbcontext has null value"**:
   - **Clarification**: `MystiraAppDbContext` is not a data entity - it's the Entity Framework DbContext class
   - **Not applicable** for migration decisions

### Missing from Discussion

The conversation didn't mention these existing systems:

1. **Polyglot Persistence Framework** - Already implemented with:
   - Circuit breaker (5 failures, 30s break)
   - Retry with exponential backoff (3 attempts)
   - Dual-write compensation
   - Consistency validation

2. **Scenario complexity** - Scenarios have deeply nested owned entities:
   - Characters with Metadata
   - Scenes with nested Media, Music, SoundEffects, Branches, EchoReveals
   - This makes them **poor PostgreSQL candidates**

3. **Partition Key Strategy** - Already optimized:
   - `/accountId` for GameSessions (hot partition per user)
   - `/profileId` for PlayerScenarioScores
   - `/mediaType` for MediaAssets
   - `/axis` for CompassTracking

---

## Recommended Migration Strategy

### Phase 1: PostgreSQL Candidates (High Value)
```
Priority 1: accounts        - Transactional, relational, FK target
Priority 2: gamesessions    - ACID required, frequent updates
Priority 3: player_scores   - Analytical queries, joins needed
```

### Phase 2: Keep in Cosmos (Architectural Fit)
```
- Scenarios (deeply nested, complex documents)
- ContentBundles (rarely updated, hierarchical)
- Configuration entities (read-heavy, schema flexible)
- Media metadata (unstructured, flexible queries)
```

### Phase 3: Consider Split
```
- UserProfiles: Core → PostgreSQL, Preferences → Cosmos
- AvatarConfiguration: Could move to PostgreSQL (simple mapping)
```

---

## Redis Usage Clarification

Current Redis configuration (`CacheOptions.cs`):
- **Not for data storage** - Pure caching layer
- **Cache-aside pattern** - Check cache first, load from DB on miss
- **Write-through** - Update cache on writes (configurable)
- **Invalidation** - Invalidate on updates/deletes
- **Fallback** - In-memory cache when Redis unavailable

This is the **correct** usage of Redis - the WhatsApp grouping of Redis with Blob was conceptually misleading.

---

## Action Items

| Priority | Action | Owner | Notes |
|----------|--------|-------|-------|
| 🔴 High | Clarify `mystiraappdbcontext` | Dev Team | It's infrastructure, not data |
| 🔴 High | Decide on `CharacterMediaMetadataFile` | Dev Team | Delete or keep in Cosmos |
| 🟡 Medium | Review UserProfile split strategy | Architect | Core vs Extended separation |
| 🟡 Medium | Test PolyglotRepository in staging | DevOps | Migration infrastructure ready |
| 🟢 Low | Document partition key rationale | Dev Team | ADR update |

---

## Conclusion

The WhatsApp discussion demonstrated **sound architectural thinking** with approximately **75% accuracy**. The key corrections needed:

1. **Current state**: Everything is Cosmos DB, not already split
2. **Redis**: Is caching, not storage - don't group with Blob
3. **Scenarios**: Should stay Cosmos due to deep nesting
4. **Infrastructure ready**: Polyglot persistence already implemented

The team's instinct to question and examine actual data before migration is the **correct approach**. The polyglot persistence framework in `PolyglotRepository<T>` provides a safe, monitored migration path.

---

## Implementation Status

**IMPLEMENTED** - PostgreSQL support has been added to the codebase:

### New Files Created

| File | Purpose |
|------|---------|
| `PostgresDbContext.cs` | PostgreSQL DbContext for migration candidates |
| `V001__Initial_Migration_Candidates.sql` | PostgreSQL schema migration script |

### Configuration Added

```json
// appsettings.json
{
  "ConnectionStrings": {
    "PostgreSql": "Host=;Database=;Username=;Password="
  },
  "PolyglotPersistence": {
    "Phase": "CosmosOnly",
    "EnableCompensation": true,
    "DualWriteTimeoutMs": 5000,
    "EnableConsistencyValidation": false
  }
}
```

### Operational Modes

The system uses **permanent polyglot persistence** (not migration):

| Mode | Description | Use Case |
|------|-------------|----------|
| `CosmosOnly` | All operations use Cosmos DB only | Initial setup, no PostgreSQL configured |
| `DualWriteCosmosRead` | Write to both, read from Cosmos | **Recommended for production** |

**Architecture:**
- **Cosmos DB**: Primary store for reads/writes, document data, global distribution
- **PostgreSQL**: Secondary store for analytics, reporting, relational queries

To enable dual-write, set:
```json
"PolyglotPersistence": {
  "Phase": "DualWriteCosmosRead"
}
```

### PostgreSQL Schema

Run the migration script at:
```
src/Mystira.App.Infrastructure.Data/Migrations/PostgreSQL/V001__Initial_Migration_Candidates.sql
```

---

## References

- `src/Mystira.App.Infrastructure.Data/MystiraAppDbContext.cs` - All 26 Cosmos containers
- `src/Mystira.App.Infrastructure.Data/PostgresDbContext.cs` - PostgreSQL migration candidates
- `src/Mystira.App.Application/Ports/Data/MigrationPhase.cs` - Migration phases
- `src/Mystira.App.Infrastructure.Data/Polyglot/PolyglotRepository.cs` - Migration infrastructure
- `src/Mystira.App.Infrastructure.Data/Caching/CacheOptions.cs` - Redis configuration
- `src/Mystira.App.Infrastructure.Data/Migrations/PostgreSQL/` - PostgreSQL migration scripts
