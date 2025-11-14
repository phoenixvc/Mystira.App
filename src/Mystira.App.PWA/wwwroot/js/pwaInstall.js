const IOS_PROMPT = Symbol('IOS_PROMPT');
let deferredPrompt = window.deferredPrompt ?? null;
let dotNetRef = null;
let displayModeMediaQuery = null;
const mobilePattern = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i;

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

function isDeviceSupported() {
    return true;
}

function isAppInstalled() {
    return window.matchMedia('(display-mode: standalone)').matches ||
        window.matchMedia('(display-mode: fullscreen)').matches ||
        window.matchMedia('(display-mode: minimal-ui)').matches ||
        window.navigator.standalone === true;
}

function updateButtonVisibility() {
    if (!dotNetRef) {
        return;
    }

    // Hide button if app is already installed
    if (isAppInstalled()) {
        console.log('PWA Install: App already installed, hiding button');
        dotNetRef.invokeMethodAsync('HideInstallButton');
        return;
    }

    // Show button if:
    // 1. We have a deferred prompt (Chrome/Edge native support)
    // 2. OR it's iOS (manual instructions)
    // 3. OR it's any mobile device (fallback with manual instructions)
    const shouldShow = deferredPrompt || isIos() || isMobile();

    if (shouldShow && isDeviceSupported()) {
        console.log('PWA Install: Showing install button', {
            hasDeferredPrompt: !!deferredPrompt,
            isIos: isIos(),
            isMobile: isMobile()
        });
        dotNetRef.invokeMethodAsync('ShowInstallButton');
    } else {
        console.log('PWA Install: Hiding install button');
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
    console.log('PWA Install: appinstalled');
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

    if (typeof displayModeMediaQuery.addEventListener === 'function') {
        displayModeMediaQuery.addEventListener('change', updateButtonVisibility);
    } else if (typeof displayModeMediaQuery.addListener === 'function') {
        displayModeMediaQuery.addListener(updateButtonVisibility);
    }
}

function unregisterDisplayModeListener() {
    if (!displayModeMediaQuery) {
        return;
    }

    if (typeof displayModeMediaQuery.removeEventListener === 'function') {
        displayModeMediaQuery.removeEventListener('change', updateButtonVisibility);
    } else if (typeof displayModeMediaQuery.removeListener === 'function') {
        displayModeMediaQuery.removeListener(updateButtonVisibility);
    }

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
            'Chrome/Samsung Internet:',
            '\u2022 Tap the menu (three dots) in the browser.',
            '\u2022 Select "Add to Home screen" or "Install app".',
            '',
            'Firefox:',
            '\u2022 Tap the home icon in the address bar.',
            '\u2022 Select "Add to Home Screen".',
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
        isInstalled: isAppInstalled()
    });

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.addEventListener('appinstalled', handleAppInstalled);
    window.addEventListener('resize', updateButtonVisibility);
    window.addEventListener('orientationchange', updateButtonVisibility);

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

    updateButtonVisibility();

    console.log('PWA Install: Initialization complete');
}

export async function installPwa() {
    if (deferredPrompt === IOS_PROMPT) {
        showIosInstallInstructions();
        return;
    }

    if (!deferredPrompt) {
        console.log('PWA Install: No deferred prompt available');
        return;
    }

    try {
        deferredPrompt.prompt();
        const choiceResult = await deferredPrompt.userChoice;
        console.log(`PWA Install: User choice - ${choiceResult.outcome}`);
    } catch (error) {
        console.error('PWA Install: Error while prompting install', error);
    }

    assignDeferredPrompt(null);
    updateButtonVisibility();
}

export function cleanup() {
    window.removeEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.removeEventListener('appinstalled', handleAppInstalled);
    window.removeEventListener('resize', updateButtonVisibility);
    window.removeEventListener('orientationchange', updateButtonVisibility);

    unregisterDisplayModeListener();

    dotNetRef = null;
    assignDeferredPrompt(null);
}
