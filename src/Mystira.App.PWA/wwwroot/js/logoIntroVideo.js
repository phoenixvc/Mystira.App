// Logo Intro Video Handler
// Loads video in background, transitions from logo to video when ready,
// then back to logo when video ends
// Supports theme-aware video loading (light/dark mode)

// Store reference per video element to support multiple instances
const videoRefs = new Map();

// Detect current theme
function getCurrentTheme() {
    // Check for explicit theme setting
    const explicitTheme = document.documentElement.getAttribute('data-theme');
    if (explicitTheme) {
        return explicitTheme;
    }

    // Fall back to system preference
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
    videoRefs.set(video, dotNetRef);

    // Set the video source based on current theme
    const themeSrc = getVideoSource();
    console.log('Loading theme-aware video:', themeSrc);

    if (videoSource) {
        videoSource.src = themeSrc;
    } else {
        // Fallback: create source element if it doesn't exist
        const source = document.createElement('source');
        source.id = 'video-source';
        source.src = themeSrc;
        source.type = 'video/mp4';
        video.appendChild(source);
    }

    // Handle video load error - stay on logo
    video.addEventListener('error', function() {
        console.log('Logo intro video failed to load, staying on static logo');
        const ref = videoRefs.get(video);
        if (ref) {
            ref.invokeMethodAsync('OnVideoError');
        }
    });

    // Handle video end - transition back to logo with animation
    video.addEventListener('ended', function() {
        console.log('Video ended, transitioning back to logo with animation');
        const ref = videoRefs.get(video);
        if (ref) {
            ref.invokeMethodAsync('OnVideoEnded');
        }
    });

    // Handle video ready to play - transition from logo to video
    video.addEventListener('canplaythrough', function() {
        console.log('Video loaded and ready to play');
        const ref = videoRefs.get(video);
        if (ref) {
            // Notify Blazor that video is ready
            ref.invokeMethodAsync('OnVideoReady');

            // Small delay to allow CSS transition to start, then play
            setTimeout(function() {
                video.play().catch(function(error) {
                    console.log('Video autoplay prevented:', error);
                    // If autoplay fails, stay on logo
                    ref.invokeMethodAsync('OnVideoError');
                });
            }, 100);
        }
    }, { once: true }); // Only trigger once on first load

    // Start loading the video
    video.load();
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
    if (videoSource && videoSource.src !== themeSrc) {
        console.log('Theme changed, updating video source to:', themeSrc);
        videoSource.src = themeSrc;
        video.load();
    }

    // Reset video to beginning
    video.currentTime = 0;

    // Play the video
    video.play().catch(function(error) {
        console.log('Video replay failed:', error);
        const ref = videoRefs.get(video);
        if (ref) {
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
        videoRefs.delete(video);
    }
};
