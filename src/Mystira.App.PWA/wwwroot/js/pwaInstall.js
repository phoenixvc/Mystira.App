const IOS_PROMPT = Symbol('IOS_PROMPT');
const INSTALL_FLAG_KEY = 'mystira_pwa_installed';
let deferredPrompt = window.deferredPrompt ?? null;
let dotNetRef = null;
let displayModeMediaQuery = null;
let userEngagementTimer = null;
let engagementTime = 0;
let hasCheckedCriteria = false; // Flag to prevent redundant criteria checks
let lastButtonState = null; // Track last button visibility state to reduce logging
const mobilePattern = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;
const MIN_ENGAGEMENT_TIME = 30000; // 30 seconds of engagement before auto-prompt

function assignDeferredPrompt(value) {
    deferredPrompt = value ?? null;
    window.deferredPrompt = deferredPrompt;
}

function isIos() {
    const ua = (navigator.userAgent || navigator.vendor || window.opera || '').toLowerCase();
    if (/iphone|ipad|ipod/.test(ua)) {
        return true;
    }

    const platform = navigator.platform || navigator.userAgentData?.platform || '';
    if (platform === 'MacIntel' && navigator.maxTouchPoints > 1) {
        return true;
    }

    // Additional iOS detection for newer devices
    if (platform === 'iPad' || platform === 'iPhone' || platform === 'iPod') {
        return true;
    }

    // Check for iOS using platform with userAgent
    if (/(iPad|iPhone|iPod)/.test(navigator.platform)) {
        return true;
    }

    return false;
}

function isMobile() {
    // Check if device is mobile or tablet
    const ua = (navigator.userAgent || navigator.vendor || window.opera || '').toLowerCase();
    if (mobilePattern.test(ua)) {
        return true;
    }

    // Check for touch device
    if ('ontouchstart' in window || navigator.maxTouchPoints > 0) {
        return true;
    }

    // Check screen size as fallback
    if (window.screen.width <= 1024) {
        return true;
    }

    return false;
}

function trackUserEngagement() {
    // Track user engagement to meet Chrome's installation criteria
    if (!userEngagementTimer) {
        userEngagementTimer = setInterval(() => {
            engagementTime += 1000;
            
            // Only log every 10 seconds to reduce console spam
            if (engagementTime % 10000 === 0) {
                console.log(`PWA Install: User engagement time: ${engagementTime / 1000}s`);
            }
            
            // After sufficient engagement, try to trigger installation if possible (only once)
            if (engagementTime >= MIN_ENGAGEMENT_TIME && !deferredPrompt && !isAppInstalled() && !hasCheckedCriteria) {
                console.log('PWA Install: Sufficient engagement reached, checking install criteria');
                hasCheckedCriteria = true; // Prevent redundant checks
                const criteriaMet = checkInstallCriteria();
                
                // If criteria are met and we're in Chrome, try to auto-install
                if (criteriaMet && navigator.userAgent.includes('Chrome') && !isIos()) {
                    console.log('PWA Install: Auto-installing after engagement');
                    setTimeout(() => {
                        if (!isAppInstalled()) {
                            installPwa(); // Attempt automatic installation
                        }
                    }, 3000); // Give user 3 seconds to see the button first
                }
            }
        }, 1000);
    }
}

function checkInstallCriteria() {
    // Check if we can trigger installation
    if (deferredPrompt && deferredPrompt !== IOS_PROMPT) {
        console.log('PWA Install: Deferred prompt available, updating button visibility');
        // Don't auto-prompt, just make button more prominent
        updateButtonVisibility();
        return true;
    }
    
    // For Chrome, try to manually trigger install if criteria might be met
    if (navigator.userAgent.includes('Chrome') && !isIos() && !isAppInstalled()) {
        // Try to trigger the install UI manually (no logging here to reduce spam)
        tryTriggerChromeInstall();
        return false;
    }
    
    return false;
}

function tryTriggerChromeInstall() {
    // Attempt to trigger Chrome's install UI
    // This is a workaround for when beforeinstallprompt doesn't fire
    // but the app meets installation criteria
    
    // Check if we're in Chrome and the page meets basic criteria
    if (window.location.protocol === 'https:' && 
        document.querySelector('link[rel="manifest"]') &&
        navigator.serviceWorker && 
        navigator.serviceWorker.controller) {
        
        // Try to show the install button after a short delay
        setTimeout(() => {
            if (!deferredPrompt && !isAppInstalled()) {
                console.log('PWA Install: Chrome criteria met, showing install button');
                updateButtonVisibility();
            }
        }, 2000);
    }
}

function stopEngagementTracking() {
    if (userEngagementTimer) {
        clearInterval(userEngagementTimer);
        userEngagementTimer = null;
    }
}

export function isAppInstalled() {
    // Check if running in standalone mode (PWA is currently running as installed app)
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches ||
        window.matchMedia('(display-mode: fullscreen)').matches ||
        window.matchMedia('(display-mode: minimal-ui)').matches ||
        window.navigator.standalone === true;
    
    // If running in standalone mode, mark as installed
    if (isStandalone) {
        localStorage.setItem(INSTALL_FLAG_KEY, 'true');
        return true;
    }
    
    // Check persistent storage flag (app was installed in a previous session)
    const wasInstalled = localStorage.getItem(INSTALL_FLAG_KEY) === 'true';
    return wasInstalled;
}

function isDeviceSupported() {
    return true;
}

function updateButtonVisibility() {
    if (!dotNetRef) {
        return;
    }

    // Hide button if app is already installed
    if (isAppInstalled()) {
        if (lastButtonState !== 'hidden-installed') {
            console.log('PWA Install: App already installed, hiding button');
            lastButtonState = 'hidden-installed';
        }
        dotNetRef.invokeMethodAsync('HideInstallButton');
        stopEngagementTracking();
        return;
    }

    // Show button if:
    // 1. We have a deferred prompt (Chrome/Edge native support)
    // 2. OR it's iOS (manual instructions)
    // 3. OR it's any mobile device (fallback with manual instructions)
    // 4. OR user has been engaged for sufficient time (try to meet Chrome criteria)
    const shouldShow = deferredPrompt || isIos() || isMobile() || engagementTime >= MIN_ENGAGEMENT_TIME;

    if (shouldShow && isDeviceSupported()) {
        if (lastButtonState !== 'shown') {
            console.log('PWA Install: Showing install button', {
                hasDeferredPrompt: !!deferredPrompt,
                isIos: isIos(),
                isMobile: isMobile(),
                engagementTime: engagementTime / 1000
            });
            lastButtonState = 'shown';
        }
        dotNetRef.invokeMethodAsync('ShowInstallButton');
    } else {
        if (lastButtonState !== 'hidden') {
            console.log('PWA Install: Hiding install button');
            lastButtonState = 'hidden';
        }
        dotNetRef.invokeMethodAsync('HideInstallButton');
    }
}

const handleBeforeInstallPrompt = (event) => {
    console.log('PWA Install: beforeinstallprompt fired');
    event.preventDefault();
    assignDeferredPrompt(event);
    updateButtonVisibility();
};

const handleAppInstalled = () => {
    console.log('PWA Install: appinstalled event fired - marking as installed');
    localStorage.setItem(INSTALL_FLAG_KEY, 'true');
    assignDeferredPrompt(null);
    updateButtonVisibility();
};

function registerDisplayModeListener() {
    if (!('matchMedia' in window)) {
        return;
    }

    displayModeMediaQuery = window.matchMedia('(display-mode: standalone)');
    if (!displayModeMediaQuery) {
        return;
    }

    // Always prefer addEventListener (supported in almost all browsers since 2022)
    if (typeof displayModeMediaQuery.addEventListener === 'function') {
        displayModeMediaQuery.addEventListener('change', updateButtonVisibility);
    }
    // Remove else branch for legacy addListener (if you do not support very old browsers)
    // else if (typeof displayModeMediaQuery.addListener === 'function') {
    //     displayModeMediaQuery.addListener(updateButtonVisibility);
    // }
}

function unregisterDisplayModeListener() {
    if (!displayModeMediaQuery) {
        return;
    }

    if (typeof displayModeMediaQuery.removeEventListener === 'function') {
        displayModeMediaQuery.removeEventListener('change', updateButtonVisibility);
    }
    // Remove else branch for legacy removeListener (if you do not support very old browsers)
    // else if (typeof displayModeMediaQuery.removeListener === 'function') {
    //     displayModeMediaQuery.removeListener(updateButtonVisibility);
    // }

    displayModeMediaQuery = null;
}

function showIosInstallInstructions() {
    let message;
    
    if (isIos()) {
        message = [
            'To install Mystira on your iOS device:',
            '\u2022 Tap the Share icon (square with an upward arrow).',
            '\u2022 Scroll down and choose "Add to Home Screen".',
            '\u2022 Confirm by tapping "Add".',
            '',
            'Once added, launch Mystira from your home screen for a full-screen experience.'
        ].join('\n');
    } else {
        // Generic instructions for other mobile browsers
        message = [
            'To install Mystira on your device:',
            '',
            'Chrome Desktop/Android:',
            '\u2022 Click the install button when prompted by Chrome, or',
            '\u2022 Look for the install icon in the address bar, or',
            '\u2022 Go to Chrome menu (three dots) > "Install app"',
            '',
            'Note: The install option appears after you\'ve used the app for a few minutes.',
            '',
            'Other browsers (Firefox, Samsung Internet, etc.):',
            '\u2022 Look for "Add to Home Screen" in the browser menu',
            '\u2022 Check for a home icon in the address bar',
            '',
            'Once installed, you can launch Mystira from your home screen.'
        ].join('\n');
    }

    window.alert(message);
}

export function initializePwaInstall(dotNetReference) {
    dotNetRef = dotNetReference;

    console.log('PWA Install: Starting initialization', {
        userAgent: navigator.userAgent,
        platform: navigator.platform,
        isIos: isIos(),
        isMobile: isMobile(),
        isInstalled: isAppInstalled(),
        protocol: window.location.protocol,
        hasServiceWorker: !!navigator.serviceWorker,
        hasManifest: !!document.querySelector('link[rel="manifest"]')
    });

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.addEventListener('appinstalled', handleAppInstalled);
    window.addEventListener('resize', updateButtonVisibility);
    window.addEventListener('orientationchange', updateButtonVisibility);
    
    // Track user engagement for Chrome installation criteria
    document.addEventListener('click', trackUserEngagement, { once: true });
    document.addEventListener('scroll', trackUserEngagement, { once: true });
    document.addEventListener('keydown', trackUserEngagement, { once: true });

    registerDisplayModeListener();

    // Restore any previously captured deferred prompt
    if (window.deferredPrompt && !deferredPrompt) {
        assignDeferredPrompt(window.deferredPrompt);
    }

    // Provide an install button experience for iOS and mobile devices
    if (!deferredPrompt && isIos() && !isAppInstalled()) {
        console.log('PWA Install: iOS device detected, setting IOS_PROMPT');
        assignDeferredPrompt(IOS_PROMPT);
    } else if (!deferredPrompt && isMobile() && !isAppInstalled()) {
        console.log('PWA Install: Mobile device detected without native prompt, showing manual instructions');
        assignDeferredPrompt(IOS_PROMPT); // Reuse iOS prompt mechanism for generic instructions
    }

    // Aggressive Chrome detection - check if criteria are met immediately
    if (navigator.userAgent.includes('Chrome') && !isIos() && !isAppInstalled()) {
        console.log('PWA Install: Chrome detected, checking install criteria immediately');
        setTimeout(() => {
            tryTriggerChromeInstall();
        }, 1000); // Check after 1 second
    }

    updateButtonVisibility();

    console.log('PWA Install: Initialization complete');
}

export async function installPwa() {
    if (deferredPrompt === IOS_PROMPT) {
        showIosInstallInstructions();
        return;
    }

    if (!deferredPrompt) {
        console.log('PWA Install: No deferred prompt available, trying to trigger install');
        // Try to trigger install manually for Chrome
        if (navigator.userAgent.includes('Chrome') && !isIos()) {
            showIosInstallInstructions(); // Show updated instructions
            return;
        }
        return;
    }

    try {
        console.log('PWA Install: Triggering native install prompt');
        await deferredPrompt.prompt();
        const choiceResult = await deferredPrompt.userChoice;
        console.log(`PWA Install: User choice - ${choiceResult.outcome}`);
        
        if (choiceResult.outcome === 'accepted') {
            console.log('PWA Install: User accepted installation - marking as installed');
            localStorage.setItem(INSTALL_FLAG_KEY, 'true');
        } else {
            console.log('PWA Install: User dismissed installation');
        }
    } catch (error) {
        console.error('PWA Install: Error while prompting install', error);
        // Fallback to manual instructions
        showIosInstallInstructions();
    }

    assignDeferredPrompt(null);
    updateButtonVisibility();
}

export function resetInstallState() {
    // Clear the install flag (useful for testing or if user uninstalls)
    console.log('PWA Install: Resetting install state');
    localStorage.removeItem(INSTALL_FLAG_KEY);
    updateButtonVisibility();
}

export function cleanup() {
    window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.removeEventListener('appinstalled', handleAppInstalled);
    window.removeEventListener('resize', updateButtonVisibility);
    window.removeEventListener('orientationchange', updateButtonVisibility);
    
    document.removeEventListener('click', trackUserEngagement);
    document.removeEventListener('scroll', trackUserEngagement);
    document.removeEventListener('keydown', trackUserEngagement);

    unregisterDisplayModeListener();
    stopEngagementTracking();

    dotNetRef = null;
    assignDeferredPrompt(null);
    hasCheckedCriteria = false; // Reset criteria check flag
    lastButtonState = null; // Reset button state tracking
}
