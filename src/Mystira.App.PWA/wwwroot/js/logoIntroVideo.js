// Logo Intro Video Handler
// Loads video in background, transitions from logo to video when ready,
// then back to logo when video ends

// Store reference per video element to support multiple instances
const videoRefs = new Map();

window.initLogoIntroVideo = function(dotNetRef) {
    const video = document.getElementById('logo-intro-video');
    const logo = document.getElementById('hero-logo-static');

    if (!video || !logo) {
        // If video element doesn't exist, stay on logo
        console.log('Video or logo element not found');
        return;
    }

    // Store the reference for this video
    videoRefs.set(video, dotNetRef);

    // Check if video source exists
    const videoSource = video.querySelector('source');
    if (!videoSource || !videoSource.src) {
        // No video source, stay on logo
        console.log('No video source found');
        return;
    }

    // Handle video load error - stay on logo
    video.addEventListener('error', function() {
        console.log('Logo intro video failed to load, staying on static logo');
        const ref = videoRefs.get(video);
        if (ref) {
            ref.invokeMethodAsync('OnVideoError');
        }
    });

    // Handle video end - transition back to logo
    video.addEventListener('ended', function() {
        console.log('Video ended, transitioning back to logo');
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

    if (!video) {
        console.log('Video element not found');
        return;
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
