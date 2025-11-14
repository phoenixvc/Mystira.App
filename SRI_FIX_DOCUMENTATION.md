# SRI Intermittent Integrity Errors Fix

## Problem Statement
Users experienced intermittent SRI (Subresource Integrity) validation failures when loading the PWA, particularly with .wasm files:
- "Failed to find a valid digest in the 'integrity' attribute for resource 'https://mystira.app/_framework/Mystira.App.PWA.wasm'"
- "SRI's integrity checks failed"
- Reloading the page typically resolved the issue

## Root Cause Analysis
The intermittent SRI failures were caused by a mismatch between:
1. **Stale HTML cache**: Service worker was caching `index.html`, which contained SRI integrity hashes
2. **Fresh framework files**: When new deployments occurred, new .wasm and framework .js files were served
3. **Hash mismatch**: The cached `index.html` contained old SRI hashes that didn't match the new framework files
4. **Brotli compression**: Azure Static Web Apps serves `.html.br` compressed versions which could also be cached with stale hashes

## Solution Overview
The fix implements a **network-first strategy** for HTML and framework files to ensure SRI hashes are always fresh:

### 1. Service Worker Changes (`service-worker.js` and `service-worker.published.js`)

#### Key Strategy Changes:
- **HTML Files**: Network-first strategy
  - Always fetch from network first
  - Fallback to cache only if network fails
  - Prevents serving stale index.html with outdated SRI hashes

- **Framework Files**: Network-first strategy
  - .wasm files: Always fetched from network first
  - blazor.webassembly.js: Always fetched from network first
  - .js.br and .html.br compressed files: Always fetched from network first

- **Other Files**: Maintained existing cache strategies
  - Icons: Cache-first (stable assets)
  - CSS, JS: Cache-first (can be invalidated through hash naming)
  - Manifest: Cache-first (stable config)

#### Updated `coreFilesToCache` Array:
Removed `index.html` and `_framework/blazor.webassembly.js` from pre-cached files to prevent stale caching.

### 2. HTML Cache Headers (`index.html`)

Added explicit cache-control meta tags to prevent HTTP caching:
```html
<meta http-equiv="cache-control" content="no-cache, no-store, must-revalidate" />
<meta http-equiv="pragma" content="no-cache" />
<meta http-equiv="expires" content="0" />
```

These headers instruct all layers of caching (browser, CDN, proxies) not to cache the HTML file.

### 3. Azure Static Web Apps Configuration (`staticwebapp.config.json`)

Created comprehensive cache control headers for all critical files:

#### No-Cache Files (always fetch from server):
- `/index.html` - Main HTML file
- `/*.html` - All HTML files
- `/*.html.br` - Brotli-compressed HTML
- `/_framework/*.wasm` - WebAssembly files
- `/_framework/*.js` - Framework JavaScript
- `/_framework/*.js.br` - Brotli-compressed JS
- `/manifest.json` - PWA manifest
- `/service-worker.js` - Service worker itself

**Cache Headers**: `no-cache, no-store, must-revalidate, max-age=0`

#### Long-Cache Files (immutable assets):
- `/css/*` - CSS files (assumed versioned)
- `/js/*` - JavaScript files (assumed versioned)
- `/icons/*` - Icon assets

**Cache Headers**: `public, max-age=31536000, immutable`

## How This Fixes the SRI Issue

### Before (Problematic Behavior):
1. User loads app → Browser caches index.html with SRI hashes (e.g., hash-v1)
2. Service worker caches index.html in service worker cache
3. New deployment occurs → Framework files updated (new .wasm files with different content)
4. User reloads app → Stale index.html served (from service worker cache or browser cache)
5. Stale index.html contains old SRI hashes (hash-v1)
6. Browser attempts to load new .wasm files
7. New .wasm files fail SRI validation (hash-v1 ≠ actual hash)
8. **Error**: SRI integrity check failed

### After (Fixed Behavior):
1. User loads app → index.html NOT cached (network-first)
2. Fresh index.html served with current SRI hashes (hash-v2)
3. Framework files loaded from network
4. SRI hashes in index.html always match the actual files
5. New deployment occurs
6. User reloads app → Fresh index.html retrieved from network
7. Fresh index.html contains correct SRI hashes
8. Framework files match their hashes
9. **Success**: No SRI errors

## Deployment Atomic Safety

The configuration ensures that all related files are updated atomically:
- `index.html` and `index.html.br` are never cached, so they're always fresh
- Framework files (.wasm, .js, .js.br) are fetched on-demand from network
- Since HTML references current framework files, they're always in sync
- No intermediate states where HTML references non-existent or mismatched files

## Browser Cache Behavior

The solution handles various caching layers:

1. **Browser HTTP Cache**: Prevented by `Cache-Control` headers in staticwebapp.config.json
2. **Browser LocalStorage**: Not used for framework files
3. **Service Worker Cache**: 
   - Removed HTML and framework files from pre-cache list
   - Network-first fetch strategy ensures fresh files
   - Service worker clears all caches on install/activate
4. **CDN/Proxy Cache**: Prevented by `no-cache` directive
5. **Brotli Compression Cache**: Same cache headers apply to .br files

## Backward Compatibility

- Changes are transparent to users
- Improved reliability for new users and installations
- Existing cached files will be naturally invalidated through the network-first strategy
- No breaking changes to the application code

## Testing the Fix

### Manual Testing:
1. Open DevTools Network tab
2. Verify index.html response headers include cache-control headers
3. Load PWA, check that index.html is fetched from network (not cache)
4. Check service worker status - should be active but not caching HTML/framework files

### Production Monitoring:
1. Monitor error logs for SRI validation failures
2. After deployment, verify errors decrease or disappear
3. Monitor network requests to ensure HTML is always fresh
4. Check that framework files load without errors

## Files Modified

1. **src/Mystira.App.PWA/wwwroot/index.html**
   - Added cache-control meta tags
   - Prevents HTTP caching of the main HTML file

2. **src/Mystira.App.PWA/wwwroot/service-worker.js**
   - Updated cache files list (removed index.html and blazor.webassembly.js)
   - Added network-first strategy for HTML and framework files
   - Added detection logic for framework files

3. **src/Mystira.App.PWA/wwwroot/service-worker.published.js**
   - Added fetch event handler
   - Implements network-first strategy for HTML and framework files
   - Maintains cache clearing behavior

4. **src/Mystira.App.PWA/wwwroot/staticwebapp.config.json** (NEW)
   - Created Azure Static Web Apps configuration
   - Defines cache headers for all resource types
   - No-cache for HTML, framework files, and service worker
   - Long-cache for versioned assets (CSS, JS, icons)

## Future Enhancements

1. **Asset Versioning**: Include hash in filenames (e.g., app.css → app.a1b2c3d4.css) to enable long-term caching
2. **Service Worker Versioning**: Auto-increment service worker version on deployment
3. **Deployment Hooks**: Ensure atomic deployment of index.html and framework files
4. **Monitoring**: Track SRI validation errors in Application Insights
5. **Fallback**: Consider showing a "Please reload" message if SRI errors occur despite fixes

## References

- [MDN: Subresource Integrity](https://developer.mozilla.org/en-US/docs/Web/Security/Subresource_Integrity)
- [Azure Static Web Apps: Configuration](https://learn.microsoft.com/en-us/azure/static-web-apps/configuration-overview)
- [Service Worker: Network-First Strategy](https://developers.google.com/web/tools/workbox/modules/workbox-strategies#network_first_network_falling_back_to_cache)
- [Cache-Control Headers](https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/Cache-Control)
