# Core Audit Findings

## 1. Bugs (Logic & External Integrations)
| ID | Description | Severity | Impact | Effort | Evidence |
|----|-------------|----------|--------|--------|----------|
| **BUG-01** | DFS Cycle Logic Flaw: Recursion in `CalculateBadgeScoresQueryHandler` adds paths on cycle detection without leaf verification. | High | Med | M | `CalculateBadgeScoresQueryHandler.cs:204` |
| **BUG-02** | WhatsApp Template Param Mapping: Logic for template parameters is outdated for Azure SDK 1.1.0+. | Med | Low | S | `WhatsAppBotService.cs:372` |
| **BUG-03** | IP Asset Parsing: `StoryProtocolClient` lacks actual ABI and log parsing logic for IP Asset extraction. | High | High | L | `StoryProtocolClient.cs:73, 109` |
| **BUG-04** | MediatR Handler Injection: `StartGameSessionCommandHandler` uses `ILogger` but mapping logic is static/coupled. | Low | Low | S | `StartGameSessionCommandHandler.cs:76` |

## 2. Performance & Structural Improvements
| ID | Description | Severity | Impact | Effort | Evidence |
|----|-------------|----------|--------|--------|----------|
| **PERF-01** | Image Cache Bloat: `imageCacheManager.js` lacks eviction/TTL strategy for Blobs in IndexedDB. | Med | High | M | `imageCacheManager.js` |
| **STR-01** | Hardcoded Badge Thresholds: `CheckAchievementsUseCase` uses constants instead of dynamic config. | Low | Med | S | `CheckAchievementsUseCase.cs:42` |
| **STR-02** | Hardcoded Explorer URLs: IP Explorer URLs are hardcoded in handlers instead of injected via Options. | Low | Low | S | `GetBundleIpStatusQueryHandler.cs:16` |

## 3. UI/UX Improvements
| ID | Description | Severity | Impact | Effort | Evidence |
|----|-------------|----------|--------|--------|----------|
| **UX-01** | Missing Loading States: Transition between music scenes in PWA lacks visual feedback. | Med | Med | S | `SceneAudioOrchestrator.cs` (logic gaps) |
| **UX-02** | Feature Flagged UI: 'Golden Tubes' in `HeroSection.razor` are commented out/disabled. | Low | Low | S | `HeroSection.razor:12` |

## 4. Refactoring Opportunities
| ID | Description | Severity | Impact | Effort | Evidence |
|----|-------------|----------|--------|--------|----------|
| **REF-01** | Misleading Naming: `IndexedDbService` is transient, not persistent. Rename to `InMemoryStore`. | Med | Med | S | `IndexedDbService.cs` |
| **REF-02** | Domain Mapping Duplication: Multiple `ToDomainModel` patterns in `YamlScenario.cs` could be simplified. | Low | Med | M | `YamlScenario.cs` |

## 5. New Features (High Value)
| Feature | Value Proposition | Integration Point |
|---------|-------------------|-------------------|
| **Real-time IP Attribution** | Transparent ownership tracking for story contributors. | `StoryProtocolClient` |
| **Advanced Character Sync** | Persistence of character choices across devices (PWA <-> Mobile). | `GameSessions` Command |
| **Adaptive Music Engine** | Dynamic cross-fading based on user engagement levels. | `AudioBus` / `SceneAudioOrchestrator` |
