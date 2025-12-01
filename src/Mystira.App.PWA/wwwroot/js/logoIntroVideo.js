// Logo Intro Video Handler
// Plays video on page load, then fades to static logo with visual effects

// Store reference per video element to support multiple instances
const videoRefs = new Map();

window.initLogoIntroVideo = function(dotNetRef) {
    const video = document.getElementById('logo-intro-video');
    const logo = document.getElementById('hero-logo-static');
    
    if (!video || !logo) {
        // If video element doesn't exist, show logo immediately
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnVideoError');
        }
        return;
    }
    
    // Store the reference for this video
    videoRefs.set(video, dotNetRef);
    
    // Check if video source exists
    const videoSource = video.querySelector('source');
    if (!videoSource || !videoSource.src) {
        // No video source, show logo immediately
        if (dotNetRef) {
            dotNetRef.invokeMethodAsync('OnVideoError');
        }
        return;
    }
    
    // Handle video load error - fallback to logo
    video.addEventListener('error', function() {
        console.log('Logo intro video failed to load, showing static logo');
        const ref = videoRefs.get(video);
        if (ref) {
            ref.invokeMethodAsync('OnVideoError');
        }
    });
    
    // Handle video end - transition to logo
    video.addEventListener('ended', function() {
        console.log('Video ended, transitioning to logo');
        const ref = videoRefs.get(video);
        if (ref) {
            ref.invokeMethodAsync('OnVideoEnded');
        }
    });
    
    // Handle video can play - ensure it starts
    video.addEventListener('canplay', function() {
        if (!video.paused) {
            return; // Already playing
        }
        video.play().catch(function(error) {
            console.log('Video autoplay prevented:', error);
            // If autoplay fails, show logo immediately
            const ref = videoRefs.get(video);
            if (ref) {
                ref.invokeMethodAsync('OnVideoError');
            }
        });
    });
    
    // Fallback: If video doesn't start playing within 3 seconds, show logo
    setTimeout(function() {
        if (video.paused && video.readyState < 3) {
            console.log('Video not playing, showing static logo');
            const ref = videoRefs.get(video);
            if (ref) {
                ref.invokeMethodAsync('OnVideoError');
            }
        }
    }, 3000);
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

// Cleanup function to remove references when component is disposed
window.cleanupLogoIntroVideo = function() {
    const video = document.getElementById('logo-intro-video');
    if (video) {
        videoRefs.delete(video);
    }
};
