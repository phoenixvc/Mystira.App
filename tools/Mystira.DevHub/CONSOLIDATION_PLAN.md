# DevHub Consolidation Plan - DRY & SOLID Improvements

## Current Issues

### Large Folders with Too Many Files
1. **services/** - 47 files (many at root level)
2. **infrastructure/** - 38 files  
3. **project-deployment/** - 17 files
4. **ui/** - 16 files

## Services Folder (47 files) - Consolidation Plan

### Root-Level Files to Move

#### Environment Components → `environment/`
- `EnvironmentBanner.tsx`
- `EnvironmentContextWarning.tsx`  
- `EnvironmentPresets.tsx`
- `EnvironmentPresetSelector.tsx`
- `EnvironmentSwitcher.tsx`

#### UI Components → `components/`
- `BuildStatusIndicator.tsx`
- `DeploymentInfo.tsx`
- `RepositoryConfig.tsx`
- `ViewModeSelector.tsx`
- `WebviewView.tsx`

#### Service Components → `components/`
- `ServiceControls.tsx` (move from root)
- `ServiceCard.tsx` (move to `card/` or keep at root as main entry)

#### Hooks → `hooks/`
- `useServiceOrder.ts` (move from root)

### Result After Consolidation
- **Before**: 47 files (many scattered at root)
- **After**: ~47 files (properly organized in subfolders)
- **Root level**: Only `ServiceList.tsx` and `LogsViewer.tsx` as main entry points

## Infrastructure Folder (38 files) - Analysis

### Components Folder Structure
- 21 component files in `infrastructure/components/`
- Many components are well-organized but could benefit from:
  - Grouping related components (tabs, dialogs, etc.)
  - Extracting shared utilities

### Opportunities
1. **Tab Components** - Group in `tabs/` subfolder:
   - InfrastructureActionsTab.tsx
   - InfrastructureHistoryTab.tsx
   - InfrastructureResourcesTab.tsx
   - InfrastructureTemplatesTab.tsx
   - InfrastructureRecommendedFixesTab.tsx

2. **Dialog Components** - Already grouped:
   - InfrastructureConfirmDialogs.tsx ✓

3. **Display Components** - Group in `display/` subfolder:
   - InfrastructureResponseDisplay.tsx
   - InfrastructureOutputPanel.tsx
   - WorkflowStatusDisplay.tsx
   - ReadyToDeployBanner.tsx

## DRY Violations to Address

### 1. Duplicate Status/Health Logic
- `getHealthIndicator()` used in multiple places
- Status badge rendering duplicated

### 2. Repeated Button Patterns
- Action buttons with similar styling/logic
- Confirm dialogs with similar structure

### 3. Shared Utility Functions
- Formatting functions duplicated across files
- Time formatting duplicated

## SOLID Improvements

### Single Responsibility Principle
- **ServiceCard.tsx** (357 lines) - Too many responsibilities
  - Should split into: Card, Logs, WebView, Controls
  
- **InfrastructureActionsTab.tsx** (232 lines) - Multiple responsibilities
  - Actions, Deployment, What-If viewing

### Open/Closed Principle
- Make components more extensible via props composition
- Use render props or children for flexibility

### Dependency Inversion
- Extract shared types/interfaces
- Use dependency injection for services

## Action Plan

### Phase 1: Services Folder Reorganization
1. Move environment components to `environment/`
2. Move UI components to `components/`
3. Move hooks to `hooks/`
4. Update all imports

### Phase 2: Infrastructure Components Grouping
1. Create `tabs/` subfolder for tab components
2. Create `display/` subfolder for display components
3. Update imports

### Phase 3: DRY - Extract Common Patterns
1. Create shared status/health utilities
2. Create shared button components
3. Create shared formatting utilities

### Phase 4: SOLID - Split Large Components
1. Split ServiceCard into smaller components
2. Split InfrastructureActionsTab
3. Extract hooks from large components

## Expected Results

### File Count Reduction
- **services/**: Better organization (same files, better structure)
- **infrastructure/components/**: Grouped into subfolders (~21 files → ~5 subfolders)
- **Overall**: Better discoverability and maintainability

### Code Quality Improvements
- Reduced duplication (DRY)
- Smaller, focused components (SOLID)
- Better testability
- Easier to maintain

