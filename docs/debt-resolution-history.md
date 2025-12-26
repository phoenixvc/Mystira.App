# Debt Resolution History

## Cycle 1: PWA Robustness & Configuration Consolidation
**Date:** 2025-12-21
**Items Resolved:**
- **DEBT-03 (Persistence):** `InMemoryStoreService` now persists its state to the browser's `localStorage` via JS Interop. This prevents data loss (e.g., UI preferences, non-sensitive session data) when the user refreshes the PWA.
- **DEBT-09 (Configuration):** Hardcoded Story Protocol Explorer URLs were removed from `GetBundleIpStatusQueryHandler` and `GetScenarioIpStatusQueryHandler`. These now use the centralized `StoryProtocolOptions.ExplorerBaseUrl`.

**Lessons Learned:**
- Blazor WASM services requiring JS interop must handle the asynchronous nature of `IJSRuntime` carefully, especially during initialization. An `EnsureInitializedAsync` pattern works well for lazy-loading stored state.
- Project references should be audited when moving logic between layers (e.g., from Infrastructure options to Application handlers).

## Cycle 2: Domain Integrity & UI Cleanup
**Date:** 2025-12-21
**Items Resolved:**
- **DEBT-02 (Logic):** Created `ScenarioGraphValidator` in the Domain layer to provide a reusable mechanism for detecting infinite loops (cycles) and unreachable nodes in scenario storyboards. This prevents "broken" content from being processed or saved.
- **DEBT-06 (UI/UX):** Cleaned up `HeroSection.razor` by removing a large block of legacy, commented-out SVG code for "Golden Lights/Tubes" that was cluttering the component and increasing maintenance surface.

## Cycle 3: SDK Modernization & Mapping Refactoring
**Date:** 2025-12-21
**Items Resolved:**
- **DEBT-04 (WhatsApp SDK):** Modernized `WhatsAppBotService.SendTemplateMessageAsync` to support positional template parameters using `MessageTemplateText` for Azure Communication Services SDK 1.1.0+. This enables dynamic template content (e.g., child names, scenario titles) in WhatsApp notifications.
- **DEBT-07 (YAML Refactoring):** Refactored `YamlScenario.ToDomainModel` and related mapping methods to reduce cyclomatic complexity. Implemented safe enum parsing with `Enum.TryParse` and explicit fallbacks, and extracted list mapping into dedicated private helper methods.

**Lessons Learned:**
- Positional parameters in Azure Communication Services WhatsApp templates require specific `MessageTemplateText` bindings (e.g., "body") which may vary by template design.
- Decoupling complex mapping logic from DTO models into dedicated helper methods significantly improves testability and readability of the domain conversion layer.

**Lessons Learned:**
- Moving logic-heavy validation (like graph traversal) into dedicated validator services keeps domain models clean while ensuring complex invariants are maintained.
- Regular cleanup of dead code (even when commented out) is essential for maintaining developer velocity and reducing cognitive load in complex UI components.

## Cycle 4: Badge Logic Decoupling & URL Audit
**Date:** 2025-12-21
**Items Resolved:**
- **DEBT-05 (Badge Logic):** Decoupled hardcoded achievement thresholds in `CheckAchievementsUseCase`. The service now injects `IRepository<BadgeConfiguration>` to fetch dynamic thresholds and metadata (Names, Messages, Icons) from the database.
- **DEBT-08 (URL Audit):** Performed a final audit of all application handlers. Verified that all Story Protocol Explorer URLs have been transitioned to use the centralized `StoryProtocolOptions.ExplorerBaseUrl`, eliminating configuration drift.

**Lessons Learned:**
- Using the Specification pattern (`AllBadgeConfigurationsSpecification`) for repository lookups maintains architectural consistency and improves query readability.
- Decoupling business rules from code into data-driven configurations significantly increases platform flexibility for future growth.
