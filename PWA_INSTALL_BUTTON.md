# PWA Install Button Implementation

## Overview
A PWA install button has been added to the Mystira application that:
- **Displays only on phones and tablets** (devices with screens ≤ 1024px and touch capability)
- **Triggers PWA installation** when clicked
- **Makes the app run chromeless** (standalone mode, without browser UI)
- **Auto-hides** when the app is already installed or on desktop devices

## Implementation Details

### Files Created/Modified

#### New Files:
1. **`src/Mystira.App.PWA/Components/PwaInstallButton.razor`**
   - Blazor component that renders the install button
   - Communicates with JavaScript via IJSRuntime
   - Shows/hides based on device capabilities and installation state

2. **`src/Mystira.App.PWA/wwwroot/js/pwaInstall.js`**
   - JavaScript ES6 module for PWA installation logic
   - Device detection (mobile/tablet vs desktop)
   - Handles `beforeinstallprompt` event
   - Monitors installation state

#### Modified Files:
1. **`src/Mystira.App.PWA/Shared/MainLayout.razor`**
   - Added `<PwaInstallButton />` component
   - Removed old inline install prompt code

2. **`src/Mystira.App.PWA/wwwroot/index.html`**
   - Removed old JavaScript install prompt code

3. **`src/Mystira.App.PWA/wwwroot/css/app.css`**
   - Updated styles for `.pwa-install-container` and `.btn-pwa-install`
   - Added mobile-responsive styles
   - Added slide-up animation
   - Added safe area insets for notched devices

4. **`src/Mystira.App.PWA/_Imports.razor`**
   - Added `@using Mystira.App.PWA.Components`

## Features

### Device Detection
The button appears only on devices that meet ALL of these criteria:
- Touch capability (detects touch events)
- Screen width ≤ 1024px (at minimum dimension)
- User agent indicates mobile/tablet device
- PWA is NOT already installed

### Installation Detection
Automatically hides the button when:
- App is running in standalone mode (`display-mode: standalone`)
- App is running in fullscreen mode
- App is running on iOS in standalone mode
- Desktop device is detected (screen > 1024px)

### PWA Chromeless Mode
The app runs chromeless (without browser chrome) due to:
- **manifest.json**: `"display": "standalone"`
- When installed via the install button, the PWA opens in standalone mode
- No browser address bar, back button, or tabs visible
- Full-screen app experience

### Button Appearance
- **Position**: Fixed at bottom-center of screen
- **Style**: Purple gradient button with shadow
- **Animation**: Slides up smoothly when shown
- **Responsive**: Adapts to different screen sizes
- **Safe areas**: Respects device notches/safe areas

## Testing

### Testing on Mobile/Tablet

#### Android (Chrome/Edge):
1. Open the PWA in Chrome/Edge browser
2. Navigate to any page
3. The install button should appear at the bottom of the screen
4. Click the button
5. Follow the browser's install prompt
6. Once installed, the app will open chromeless
7. The button should no longer appear after installation

#### iOS (Safari):
**Note**: Safari does not support the `beforeinstallprompt` event. Users must use the native "Add to Home Screen" feature:
1. Open the PWA in Safari
2. Tap the Share button
3. Select "Add to Home Screen"
4. The app will be added to the home screen and run chromeless

### Testing on Desktop
1. Open the PWA in a desktop browser
2. The install button should NOT appear (hidden by CSS media query)
3. Use browser's built-in install feature (in address bar) if needed

### Testing Installation State
1. Install the PWA using the button
2. Close the installed app
3. Reopen in browser
4. The button should NOT appear (detects already installed)

### Testing in Development

To simulate mobile view in desktop browser:
1. Open DevTools (F12)
2. Toggle device toolbar (Ctrl+Shift+M)
3. Select a mobile device preset (e.g., iPhone 12, Pixel 5)
4. Refresh the page
5. The button should appear if the `beforeinstallprompt` event fires

**Note**: Some browsers may not fire `beforeinstallprompt` in DevTools mobile simulation. Test on actual devices for best results.

## Browser Support

### Full Support (Install Button Visible):
- ✅ Chrome/Edge on Android
- ✅ Samsung Internet
- ✅ Opera on Android
- ✅ Chrome/Edge on Windows tablets with touch

### Partial Support (Native Install Only):
- ⚠️ Safari on iOS/iPadOS (use "Add to Home Screen")
- ⚠️ Firefox (limited PWA support)

### Not Shown (By Design):
- ❌ Desktop browsers on non-touch devices
- ❌ Large screens (> 1024px width)

## Technical Architecture

### Component Lifecycle:
1. **OnAfterRenderAsync**: Loads JavaScript module, initializes detection
2. **ShowInstallButton**: Called by JS when conditions are met
3. **InstallPwaAsync**: Triggered on button click, calls JS install function
4. **DisposeAsync**: Cleanup, removes event listeners

### Event Flow:
```
Browser fires beforeinstallprompt
    ↓
JS module captures event
    ↓
Device & installation checks
    ↓
JS calls ShowInstallButton() in Blazor
    ↓
Blazor renders button
    ↓
User clicks button
    ↓
Blazor calls installPwa() in JS
    ↓
JS triggers browser install prompt
    ↓
User accepts/rejects
    ↓
Button hides if accepted
```

## Customization

### Change Button Position:
Edit `.pwa-install-container` in `app.css`:
```css
.pwa-install-container {
    bottom: 1.25rem; /* Change this */
    left: 50%;       /* Change this */
}
```

### Change Button Style:
Edit `.btn-pwa-install` in `app.css`

### Change Device Detection Threshold:
Edit `isMobileOrTablet()` in `pwaInstall.js`:
```javascript
const minViewport = Math.min(window.innerWidth, window.innerHeight);
return minViewport <= 1024; // Change this value
```

### Change Media Query Breakpoint:
Edit in `app.css`:
```css
@media (min-width: 1025px) { /* Change this */
    .pwa-install-container {
        display: none !important;
    }
}
```

## Troubleshooting

### Button Not Appearing:
1. Check browser console for errors
2. Verify you're on a mobile/tablet device
3. Ensure PWA is not already installed
4. Check that site is served over HTTPS (required for PWA)
5. Verify `manifest.json` is loaded correctly

### Button Appears on Desktop:
1. Check CSS media query in `app.css`
2. Verify JavaScript device detection logic
3. Check browser window size

### Installation Not Working:
1. Verify service worker is registered
2. Check manifest.json is valid
3. Ensure site meets PWA criteria (HTTPS, icons, etc.)
4. Check browser console for errors

## Future Enhancements

Possible improvements:
- Add "Dismiss" button to hide the prompt temporarily
- Add localStorage to remember user dismissals
- Customize message for different platforms
- Add A2HS (Add to Home Screen) instructions for iOS
- Add telemetry to track install button clicks
