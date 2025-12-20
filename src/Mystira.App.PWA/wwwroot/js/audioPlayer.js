// audioEngine implementation for Mystira
(function () {
    const TRANSITION_TIMES = {
        'HardCut': 0,
        'CrossfadeShort': 500,
        'CrossfadeNormal': 1500,
        'CrossfadeLong': 4000,
        'Auto': 1500
    };

    class AudioEngineInstance {
        constructor() {
            this.music1 = new Audio();
            this.music2 = new Audio();
            this.activeMusic = this.music1;
            this.fadingMusic = this.music2;
            this.music1.loop = true;
            this.music2.loop = true;

            this.currentMusicTrackId = null;
            this.targetVolume = 1.0;
            this.baseVolume = 1.0; // The "energy" volume

            this.sfxChannels = new Map(); // trackId -> Audio
            this.isPaused = false;
            this.musicPaused = false;
        }

        async playMusic(trackUrl, transitionType, volume = 1.0) {
            if (this.currentMusicTrackId === trackUrl) {
                if (this.musicPaused) {
                    this.resumeMusic();
                }
                this.setMusicVolume(volume);
                return;
            }

            const duration = TRANSITION_TIMES[transitionType] || 1500;
            this.baseVolume = volume;
            this.currentMusicTrackId = trackUrl;

            // Swap roles
            const oldActive = this.activeMusic;
            const newActive = this.fadingMusic;
            this.activeMusic = newActive;
            this.fadingMusic = oldActive;

            // Setup new active
            newActive.src = trackUrl;
            newActive.volume = 0;

            if (!this.isPaused) {
                try {
                    await newActive.play();
                } catch (e) {
                    console.warn("Audio playback failed (interaction required?):", e);
                }
            }

            this._fade(newActive, volume, duration);
            this._fade(this.fadingMusic, 0, duration, () => {
                this.fadingMusic.pause();
                this.fadingMusic.src = "";
            });
        }

        stopMusic(transitionType) {
            const duration = TRANSITION_TIMES[transitionType] || 1500;
            this.currentMusicTrackId = null;
            this._fade(this.activeMusic, 0, duration, () => {
                this.activeMusic.pause();
                this.activeMusic.src = "";
            });
        }

        async playSfx(trackUrl, loop = false, volume = 1.0) {
            let sfx = this.sfxChannels.get(trackUrl);
            if (sfx) {
                if (loop) return; // Already playing
                sfx.pause();
                sfx.currentTime = 0;
            } else {
                sfx = new Audio(trackUrl);
                this.sfxChannels.set(trackUrl, sfx);
            }

            sfx.loop = loop;
            sfx.volume = volume;

            if (!this.isPaused) {
                try {
                    await sfx.play();
                } catch (e) {
                    console.warn("SFX playback failed:", e);
                }
            }

            if (!loop) {
                sfx.onended = () => {
                    this.sfxChannels.delete(trackUrl);
                };
            }
        }

        stopSfx(trackUrl) {
            const sfx = this.sfxChannels.get(trackUrl);
            if (sfx) {
                sfx.pause();
                this.sfxChannels.delete(trackUrl);
            }
        }

        setMusicVolume(volume, durationSeconds = 0.5) {
            this.baseVolume = volume;
            this._fade(this.activeMusic, volume, durationSeconds * 1000);
        }

        duckMusic(duck, duckVolume = 0.2) {
            const target = duck ? duckVolume : this.baseVolume;
            this._fade(this.activeMusic, target, 500);
        }

        pauseAll() {
            this.isPaused = true;
            this.activeMusic.pause();
            this.fadingMusic.pause();
            for (let sfx of this.sfxChannels.values()) {
                sfx.pause();
            }
        }

        resumeAll() {
            this.isPaused = false;
            if (this.activeMusic.src && !this.musicPaused) this.activeMusic.play().catch(() => {});
            for (let sfx of this.sfxChannels.values()) {
                if (sfx.loop || !sfx.ended) {
                    sfx.play().catch(() => {});
                }
            }
        }

        pauseMusic() {
            this.musicPaused = true;
            this.activeMusic.pause();
            this.fadingMusic.pause();
        }

        resumeMusic() {
            this.musicPaused = false;
            if (this.activeMusic.src && !this.isPaused) {
                this.activeMusic.play().catch(() => {});
            }
        }

        isMusicPaused() {
            return this.musicPaused || false;
        }

        _fade(audio, targetVolume, duration, onComplete) {
            if (!audio || !audio.src) {
                if (onComplete) onComplete();
                return;
            }

            if (duration <= 0) {
                audio.volume = targetVolume;
                if (onComplete) onComplete();
                return;
            }

            const startVolume = audio.volume;
            const startTime = performance.now();

            const tick = (now) => {
                const elapsed = now - startTime;
                const progress = Math.min(elapsed / duration, 1);

                audio.volume = startVolume + (targetVolume - startVolume) * progress;

                if (progress < 1) {
                    requestAnimationFrame(tick);
                } else {
                    if (onComplete) onComplete();
                }
            };

            requestAnimationFrame(tick);
        }
    }

    window.AudioEngine = new AudioEngineInstance();
})();

// Keep previous exports for compatibility if needed, though they might conflict if they use the same variables.
// Actually it's better to just provide them as wrappers to the new engine.
export function playAudio(url) { window.AudioEngine.playSfx(url, false, 1.0); }
export function stopAudio() { /* how to stop "anonymous" sfx? we don't have the url here usually */ }
export function isPlaying() { return false; }
