# Environment Management & UI/UX Improvements

## Current State

**Environment URLs are currently hardcoded** in `getEnvironmentUrls()`. They are NOT populated from deployment pipelines, but the infrastructure exists to fetch them dynamically.

## Proposed Improvements

### 1. Dynamic Environment URL Fetching ✅ (Partially Implemented)

**Current:** Hardcoded URLs in `getEnvironmentUrls()`

**Improvement:** Fetch from Azure App Services via `get_azure_resources` command
- Parse Azure App Service resources to extract URLs
- Map resource names to service names (api, admin-api, pwa)
- Fallback to hardcoded defaults if Azure fetch fails
- Cache results and refresh periodically

**Status:** Code added but needs integration in `useEffect`

### 2. Environment Health Status Indicators

**Add real-time health checks:**
- Show green/yellow/red indicators next to environment buttons
- Check `/health` endpoint when switching environments
- Display last checked timestamp
- Auto-refresh health status every 30 seconds

**Visual:**
```
[Local] [Dev 🟢] [PROD 🔴]  ← Green = online, Red = offline, Yellow = checking
```

### 3. Enhanced Environment Banner

**Current:** Simple yellow banner with text

**Improvements:**
- **Color-coded by environment mix:**
  - All Local: Green banner
  - Mixed: Yellow banner (warning)
  - Any Prod: Red banner (danger)
- **Click to expand:** Show detailed environment info
- **Quick actions:** "Switch all to Local" button
- **Status indicators:** Show which environments are online/offline

### 4. Environment Switcher UX Enhancements

**Current Issues:**
- Buttons are small and hard to distinguish
- No visual feedback when switching
- No indication of which environments are available

**Improvements:**
- **Larger, more prominent buttons** with icons
- **Tooltips** showing full URL and last health check
- **Disabled state** with explanation (e.g., "Service must be stopped")
- **Loading state** when checking health
- **Confirmation for Prod** with checkbox "I understand the risks"
- **Visual distinction:** 
  - Local: 🏠 Home icon, green
  - Dev: 🧪 Flask icon, blue  
  - Prod: ⚠️ Warning icon, red with pulsing animation

### 5. Service Card Environment Display

**Add to each service card:**
- **Environment badge** next to service name (more prominent)
- **Current URL display** when not local (with copy button)
- **Environment status** (online/offline/checking)
- **Last deployment info** (if available from GitHub Actions)
- **Quick switch dropdown** instead of buttons (saves space)

### 6. Environment Context Awareness

**Show warnings when:**
- Switching to Prod while other services are on Dev/Local
- Mixing environments that shouldn't be mixed
- Deployed environment is offline
- Local service is running but environment is set to deployed

**Smart defaults:**
- Remember last used environment per service
- Suggest environment based on current branch (dev branch → dev env)
- Auto-switch to local when starting a service

### 7. Deployment Pipeline Integration

**Fetch from:**
- **GitHub Actions:** Get latest deployment URLs from workflow runs
- **Azure App Services:** Query actual deployed URLs
- **Config files:** Read from `mystira-app-{env}-config.json` if available

**Display:**
- Last deployment timestamp
- Deployment status (success/failed)
- Link to GitHub Actions run
- Deployment commit/branch info

### 8. Environment Comparison View

**New feature:** Side-by-side comparison
- Compare Local vs Dev vs Prod
- Show differences in responses
- Health check comparison
- Response time comparison

### 9. Environment Presets/Profiles

**Save common configurations:**
- "Full Local" - All services local
- "API Dev, Rest Local" - API on dev, others local
- "Full Dev" - All services on dev
- "Testing Prod" - All services on prod (with extra warnings)

**Quick switch between presets**

### 10. Visual Improvements

**Environment Banner:**
- Make it sticky at top (always visible when scrolling)
- Add subtle animation when environment changes
- Show count of services per environment
- Add "Reset All" button

**Service Cards:**
- Environment indicator as a colored border/glow
- More prominent when not local
- Show environment in service title: "Admin API (DEV)"
- Add environment icon in status area

**Color Scheme:**
- Local: Green (#10B981)
- Dev: Blue (#3B82F6)  
- Prod: Red (#EF4444) with warning icon

### 11. Keyboard Shortcuts

- `Ctrl+E` - Open environment switcher
- `Ctrl+Shift+L` - Switch all to Local
- `Ctrl+Shift+D` - Switch all to Dev (with confirmation)
- `Ctrl+Shift+P` - Switch all to Prod (with double confirmation)

### 12. Environment Status Dashboard

**New tab/view:**
- Grid showing all services × all environments
- Health status for each combination
- Response times
- Last checked timestamps
- Quick actions to switch

### 13. Smart Environment Detection

**Auto-detect based on:**
- Current Git branch (dev → dev env, main → prod env)
- Time of day (prod disabled during business hours?)
- User preference
- Service dependencies (if API is on dev, suggest Admin API on dev too)

### 14. Environment Lock/Protection

**Prevent accidental changes:**
- Lock environment (requires unlock to change)
- Require password for Prod switches
- Audit log of environment changes
- Notifications when environment changes

### 15. URL Validation & Testing

**Before switching:**
- Validate URL is reachable
- Test health endpoint
- Show response time
- Display error if unreachable

**After switching:**
- Auto-open in browser/webview
- Show connection status
- Display any errors

## Implementation Priority

### High Priority (Immediate Impact)
1. ✅ Dynamic URL fetching from Azure
2. Environment health status indicators
3. Enhanced environment banner (color-coded, sticky)
4. Better environment switcher UI (larger buttons, icons, tooltips)

### Medium Priority (Better UX)
5. Service card environment display improvements
6. Environment context warnings
7. Deployment pipeline integration
8. Environment presets/profiles

### Low Priority (Nice to Have)
9. Environment comparison view
10. Environment status dashboard
11. Smart environment detection
12. Environment lock/protection

## Technical Implementation Notes

### Fetching URLs from Azure
```typescript
// Already have get_azure_resources command
// Need to parse App Service resources and extract URLs
// Map resource names to service names intelligently
```

### Health Checking
```typescript
// Use fetch with HEAD request to /health endpoint
// Cache results for 30 seconds
// Show loading state while checking
// Handle CORS issues gracefully
```

### State Management
```typescript
// Add environmentUrls state (fetched from Azure)
// Add environmentStatus state (health checks)
// Persist to localStorage
// Sync with Azure resources periodically
```

