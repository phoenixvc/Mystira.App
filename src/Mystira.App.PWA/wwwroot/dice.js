window.diceAudio = {
    play: function (audioElement) {
        if (audioElement) {
            audioElement.play();
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
        // Always force light mode
        document.documentElement.setAttribute('data-theme', 'light');
    }
};
