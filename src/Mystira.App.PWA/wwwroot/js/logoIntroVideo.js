// Logo Intro Video Handler
// Loads video in background, transitions from logo to video when ready,
// then back to logo when video ends
// Supports theme-aware video loading (light/dark mode)

// Store reference per video element to support multiple instances
// Always store an object: { dotNetRef, observer }
const videoRefs = new Map();

function setVideoRef(video, dotNetRef, observer = null) {
    videoRefs.set(video, { dotNetRef, observer });
}

function getDotNetRef(video) {
    const entry = videoRefs.get(video);
    if (!entry) return null;
    // Backward compatibility if older shape was stored
    return entry.dotNetRef ? entry.dotNetRef : entry;
}

function getObserver(video) {
    const entry = videoRefs.get(video);
    return entry && entry.observer ? entry.observer : null;
}

// Detect current theme
function getCurrentTheme() {
    // Highest priority: persisted preference
    try {
        const stored = window.localStorage ? localStorage.getItem('theme') : null;
        if (stored === 'dark' || stored === 'light') {
            return stored;
        }
    } catch (_) {
        // ignore storage errors
    }

    // Next: explicit theme attribute on <html>
    const explicitTheme = document.documentElement.getAttribute('data-theme');
    if (explicitTheme) {
        return explicitTheme;
    }

    // Fallback: system preference
    if (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches) {
        return 'dark';
    }

    return 'light';
}

// Get the appropriate video source based on theme
function getVideoSource() {
    const theme = getCurrentTheme();
    // Use theme-specific videos: hero-intro-light.mp4 or hero-intro-dark.mp4
    return `videos/hero-intro-${theme}.mp4`;
}

window.initLogoIntroVideo = function(dotNetRef) {
    const video = document.getElementById('logo-intro-video');
    const videoSource = document.getElementById('video-source');

    if (!video) {
        // If video element doesn't exist, stay on logo
        console.log('Video element not found');
        return;
    }

    // Store the reference for this video
    setVideoRef(video, dotNetRef);

    // Helper to update the video source when theme changes
    function updateVideoSourceIfChanged() {
        const themeSrc = getVideoSource();
        const currentSrc = videoSource ? videoSource.getAttribute('src') : '';
        if (currentSrc !== themeSrc) {
            console.log('Updating theme-aware video src ->', themeSrc);
            if (videoSource) {
                videoSource.setAttribute('src', themeSrc);
            } else {
                const source = document.createElement('source');
                source.id = 'video-source';
                source.src = themeSrc;
                source.type = 'video/mp4';
                video.appendChild(source);
            }
            // Reload the video to apply new source
            video.load();
        }
    }

    // Initial set based on current theme
    updateVideoSourceIfChanged();

    // Handle video load error - stay on logo
    video.addEventListener('error', function() {
        console.log('Logo intro video failed to load, staying on static logo');
        const ref = getDotNetRef(video);
        if (ref && typeof ref.invokeMethodAsync === 'function') {
            ref.invokeMethodAsync('OnVideoError');
        }
    });

    // Handle video end - transition back to logo with animation
    video.addEventListener('ended', function() {
        console.log('Video ended, transitioning back to logo with animation');
        const ref = getDotNetRef(video);
        if (ref && typeof ref.invokeMethodAsync === 'function') {
            ref.invokeMethodAsync('OnVideoEnded');
        }
    });

    // Handle video ready to play - transition from logo to video
    video.addEventListener('canplaythrough', function() {
        console.log('Video loaded and ready to play');
        const ref = getDotNetRef(video);
        if (ref && typeof ref.invokeMethodAsync === 'function') {
            // Notify Blazor that video is ready
            ref.invokeMethodAsync('OnVideoReady');

            // Small delay to allow CSS transition to start, then play
            setTimeout(function() {
                video.play().catch(function(error) {
                    console.log('Video autoplay prevented:', error);
                    // If autoplay fails, stay on logo
                    try {
                        ref.invokeMethodAsync('OnVideoError');
                    } catch (_) {
                        // ignore
                    }
                });
            }, 100);
        }
    }, { once: true }); // Only trigger once on first load

    // Start loading the video (after source set)
    video.load();

    // Watch for theme changes (via data-theme attribute changes)
    try {
        const observer = new MutationObserver((mutations) => {
            for (const m of mutations) {
                if (m.type === 'attributes' && m.attributeName === 'data-theme') {
                    updateVideoSourceIfChanged();
                }
            }
        });
        observer.observe(document.documentElement, { attributes: true, attributeFilter: ['data-theme'] });
        // Keep a reference to disconnect on cleanup
        setVideoRef(video, dotNetRef, observer);
    } catch (e) {
        // MutationObserver not available; ignore
        setVideoRef(video, dotNetRef);
    }
};

// Function to replay the logo video with transition effect
window.replayLogoVideo = function() {
    const video = document.getElementById('logo-intro-video');
    const videoSource = document.getElementById('video-source');

    if (!video) {
        console.log('Video element not found');
        return;
    }

    // Update video source in case theme changed
    const themeSrc = getVideoSource();
    if (videoSource && videoSource.getAttribute('src') !== themeSrc) {
        console.log('Theme changed, updating video source to:', themeSrc);
        videoSource.setAttribute('src', themeSrc);
        video.load();
    }

    // Reset video to beginning
    video.currentTime = 0;

    // Play the video
    video.play().catch(function(error) {
        console.log('Video replay failed:', error);
        const ref = getDotNetRef(video);
        if (ref && typeof ref.invokeMethodAsync === 'function') {
            ref.invokeMethodAsync('OnVideoError');
        }
    });
};

// Function to skip the logo video
window.skipLogoVideo = function() {
    const video = document.getElementById('logo-intro-video');

    if (!video) {
        console.log('Video element not found');
        return;
    }

    // Pause and reset video
    video.pause();
    video.currentTime = 0;
};

// Cleanup function to remove references when component is disposed
window.cleanupLogoIntroVideo = function() {
    const video = document.getElementById('logo-intro-video');
    if (video) {
        const observer = getObserver(video);
        try {
            if (observer && typeof observer.disconnect === 'function') {
                observer.disconnect();
            }
        } catch (_) {
            // ignore
        }
        videoRefs.delete(video);
    }
};
