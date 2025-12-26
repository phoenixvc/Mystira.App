window.diceAudio = {
    rollSound: null,
    
    play: function (audioElement) {
        if (audioElement) {
            audioElement.play();
        }
    },
    
    playRoll: function (audioSrc) {
        // Stop any existing roll sound
        this.stopRoll();
        
        // Create new audio element for dice roll
        this.rollSound = new Audio(audioSrc);
        this.rollSound.loop = true;
        this.rollSound.volume = 0.7; // Set energy to 0.7
        
        // Play the sound
        this.rollSound.play().catch(function(err) {
            console.warn('Dice roll sound playback failed:', err);
        });
    },
    
    stopRoll: function () {
        if (this.rollSound) {
            this.rollSound.pause();
            this.rollSound.currentTime = 0;
            this.rollSound = null;
        }
    }
};

window.diceHaptics = {
    vibrate: function (patternMs) {
        if (navigator.vibrate) {
            navigator.vibrate(patternMs);
        }
    }
};

window.diceTheme = {
    set: function (isDark) {
        document.documentElement.classList.toggle('dark-mode', isDark);
    }
};
