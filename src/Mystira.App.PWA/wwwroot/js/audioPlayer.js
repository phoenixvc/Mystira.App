// audioPlayer.js
let audioElement = null;

export function playAudio(url) {
    // Clean up previous audio element if it exists
    if (audioElement) {
        stopAudio();
    }

    // Create a new audio element
    audioElement = new Audio(url);

    // Set up event handlers
    audioElement.onended = () => {
        audioElement = null;
        // Notify Blazor component that playback has ended
        if (window.DotNet) {
            DotNet.invokeMethodAsync('YourAppAssemblyName', 'OnAudioEnded');
        }
    };

    // Start playback
    audioElement.play().catch(error => {
        console.error("Error playing audio:", error);
    });
}

export function stopAudio() {
    if (audioElement) {
        audioElement.pause();
        audioElement.currentTime = 0;
        audioElement = null;
    }
}

export function isPlaying() {
    return audioElement !== null && !audioElement.paused;
}