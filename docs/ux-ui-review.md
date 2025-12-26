# UX/UI Review & Snapshot Plan

## Critical User Flows
1. **Scenario Selection:** Browsing and selecting a story bundle.
2. **Interactive Playback:** Reading a scene and making a branch choice.
3. **Badge Achievement:** Earning a badge after completing a path.
4. **Music Orchestration:** Audio transitions when switching scenes.

## Playwright Snapshot Plan
- **Deterministic Viewport:** Fixed at 1280x720 (Desktop) and 390x844 (iPhone 12).
- **Stable Selectors:** Use `data-testid` attributes (e.g., `data-testid="branch-option"`, `data-testid="badge-popup"`).
- **Mocking Strategy:** Mock `IContentBundleClient` and `IScenarioClient` to return fixed YAML payloads.

### CI Execution
- Snapshot tests should run on every PR in GitHub Actions.
- Failure threshold: 0.5% pixel difference for UI components, 0% for layout structure.

## UI Deviations
- **Loading Indicators:** Lacking in `SceneAudioOrchestrator` transitions.
- **Button Consistency:** Some secondary buttons in `HeroSection` do not strictly follow the `#7c3aed` border-radius rules.
