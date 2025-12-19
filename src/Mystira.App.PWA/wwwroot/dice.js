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
        document.documentElement.classList.toggle('dark-mode', isDark);
    }
};
