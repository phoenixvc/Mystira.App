// Logo Intro Video Handler
// Plays video on page load, then fades to static logo

window.initLogoIntroVideo = function() {
    const video = document.getElementById('logo-intro-video');
    const logo = document.getElementById('hero-logo-static');
    
    if (!video || !logo) {
        // If video element doesn't exist, show logo immediately
        if (logo) {
            logo.style.opacity = '1';
        }
        return;
    }
    
    // Check if video source exists
    const videoSource = video.querySelector('source');
    if (!videoSource || !videoSource.src) {
        // No video source, show logo immediately
        logo.style.opacity = '1';
        video.style.display = 'none';
        return;
    }
    
    // Handle video load error - fallback to logo
    video.addEventListener('error', function() {
        console.log('Logo intro video failed to load, showing static logo');
        video.style.display = 'none';
        logo.style.opacity = '1';
    });
    
    // Handle video end - fade to logo
    video.addEventListener('ended', function() {
        fadeToLogo();
    });
    
    // Handle video can play - ensure it starts
    video.addEventListener('canplay', function() {
        video.play().catch(function(error) {
            console.log('Video autoplay prevented:', error);
            // If autoplay fails, show logo immediately
            fadeToLogo();
        });
    });
    
    // Fallback: If video doesn't start playing within 3 seconds, show logo
    setTimeout(function() {
        if (video.paused && video.readyState < 3) {
            console.log('Video not playing, showing static logo');
            fadeToLogo();
        }
    }, 3000);
    
    function fadeToLogo() {
        // Fade out video
        video.style.opacity = '0';
        
        // After fade out completes, hide video and show logo
        setTimeout(function() {
            video.style.display = 'none';
            logo.style.opacity = '1';
        }, 1500); // Match CSS transition duration
    }
};

