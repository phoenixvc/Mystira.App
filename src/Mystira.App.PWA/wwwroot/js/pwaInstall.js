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
    return platform === 'MacIntel' && navigator.maxTouchPoints > 1;
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

    if (deferredPrompt && isDeviceSupported() && !isAppInstalled()) {
        dotNetRef.invokeMethodAsync('ShowInstallButton');
    } else {
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
    const message = [
        'To install Mystira on your device:',
        '\u2022 Tap the share icon (square with an upward arrow).',
        '\u2022 Choose "Add to Home Screen".',
        '\u2022 Confirm by tapping "Add".',
        '',
        'Once added, launch Mystira from your home screen for a full-screen, chromeless experience.'
    ].join('\n');

    window.alert(message);
}

export function initializePwaInstall(dotNetReference) {
    dotNetRef = dotNetReference;

    window.addEventListener('beforeinstallprompt', handleBeforeInstallPrompt);
    window.addEventListener('appinstalled', handleAppInstalled);
    window.addEventListener('resize', updateButtonVisibility);
    window.addEventListener('orientationchange', updateButtonVisibility);

    registerDisplayModeListener();

    // Restore any previously captured deferred prompt
    if (window.deferredPrompt && !deferredPrompt) {
        assignDeferredPrompt(window.deferredPrompt);
    }

    // Provide an install button experience for iOS devices
    if (!deferredPrompt && isIos() && !isAppInstalled()) {
        assignDeferredPrompt(IOS_PROMPT);
    }

    updateButtonVisibility();

    console.log('PWA Install: initialization complete');
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
