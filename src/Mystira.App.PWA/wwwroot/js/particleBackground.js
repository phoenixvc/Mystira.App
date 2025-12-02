/**
 * Mystira Particle Background Effect
 * Creates an interactive, mouse-controlled particle system
 * with colors matching the Mystira brand theme
 */

(function() {
    'use strict';

    // Particle configuration
    const CONFIG = {
        particleCount: 1,
        connectionDistance: 120,
        mouseRadius: 150,
        particleSpeed: 0.3,
        colors: [
            'rgba(147, 51, 234, 0.35)',   // Primary purple #9333ea (lighter)
            'rgba(139, 92, 246, 0.3)',    // Accent purple #8B5CF6 (lighter)
            'rgba(124, 58, 237, 0.32)',   // Secondary #7C3AED (lighter)
            'rgba(109, 40, 217, 0.3)',    // Deeper purple #6D28D9 (lighter)
            'rgba(167, 139, 250, 0.28)'   // Lighter purple for variety (lighter)
        ],
        minSize: 1,
        maxSize: 3,
        lineOpacity: 0.08
    };

    class Particle {
        constructor(canvas) {
            this.canvas = canvas;
            this.reset();
        }

        reset() {
            this.x = Math.random() * this.canvas.width;
            this.y = Math.random() * this.canvas.height;
            this.vx = (Math.random() - 0.5) * CONFIG.particleSpeed * 2;
            this.vy = (Math.random() - 0.5) * CONFIG.particleSpeed * 2;
            this.size = Math.random() * (CONFIG.maxSize - CONFIG.minSize) + CONFIG.minSize;
            this.color = CONFIG.colors[Math.floor(Math.random() * CONFIG.colors.length)];
            this.originalX = this.x;
            this.originalY = this.y;
        }

        update(mouseX, mouseY, isMouseOver) {
            // Regular movement
            this.x += this.vx;
            this.y += this.vy;

            // Bounce off walls
            if (this.x < 0 || this.x > this.canvas.width) {
                this.vx *= -1;
                this.x = Math.max(0, Math.min(this.canvas.width, this.x));
            }
            if (this.y < 0 || this.y > this.canvas.height) {
                this.vy *= -1;
                this.y = Math.max(0, Math.min(this.canvas.height, this.y));
            }

            // Mouse interaction - particles are attracted to/repelled from mouse
            if (isMouseOver && mouseX !== null && mouseY !== null) {
                const dx = mouseX - this.x;
                const dy = mouseY - this.y;
                const distance = Math.sqrt(dx * dx + dy * dy);

                if (distance < CONFIG.mouseRadius) {
                    // Gentle push away from mouse cursor
                    const force = (CONFIG.mouseRadius - distance) / CONFIG.mouseRadius;
                    const angle = Math.atan2(dy, dx);
                    this.x -= Math.cos(angle) * force * 2;
                    this.y -= Math.sin(angle) * force * 2;
                }
            }
        }

        draw(ctx) {
            ctx.beginPath();
            ctx.arc(this.x, this.y, this.size, 0, Math.PI * 2);
            ctx.fillStyle = this.color;
            ctx.fill();
            
            // Add subtle glow effect
            ctx.shadowBlur = 10;
            ctx.shadowColor = this.color;
            ctx.fill();
            ctx.shadowBlur = 0;
        }
    }

    class ParticleSystem {
        constructor(canvasId) {
            this.canvas = document.getElementById(canvasId);
            if (!this.canvas) {
                console.warn('ParticleBackground: Canvas not found');
                return;
            }

            this.ctx = this.canvas.getContext('2d');
            this.particles = [];
            this.mouseX = null;
            this.mouseY = null;
            this.isMouseOver = false;
            this.animationId = null;
            this.isRunning = false;

            this.init();
        }

        init() {
            this.resize();
            this.createParticles();
            this.bindEvents();
            this.start();
        }

        resize() {
            const container = this.canvas.parentElement;
            if (container) {
                this.canvas.width = container.offsetWidth;
                this.canvas.height = container.offsetHeight;
            }
        }

        createParticles() {
            this.particles = [];
            for (let i = 0; i < CONFIG.particleCount; i++) {
                this.particles.push(new Particle(this.canvas));
            }
        }

        bindEvents() {
            // Resize handler with debounce
            let resizeTimeout;
            window.addEventListener('resize', () => {
                clearTimeout(resizeTimeout);
                resizeTimeout = setTimeout(() => {
                    this.resize();
                    this.createParticles();
                }, 250);
            });

            // Mouse tracking
            this.canvas.addEventListener('mousemove', (e) => {
                const rect = this.canvas.getBoundingClientRect();
                this.mouseX = e.clientX - rect.left;
                this.mouseY = e.clientY - rect.top;
            });

            this.canvas.addEventListener('mouseenter', () => {
                this.isMouseOver = true;
            });

            this.canvas.addEventListener('mouseleave', () => {
                this.isMouseOver = false;
                this.mouseX = null;
                this.mouseY = null;
            });

            // Touch support for mobile
            this.canvas.addEventListener('touchmove', (e) => {
                if (e.touches.length > 0) {
                    const rect = this.canvas.getBoundingClientRect();
                    this.mouseX = e.touches[0].clientX - rect.left;
                    this.mouseY = e.touches[0].clientY - rect.top;
                    this.isMouseOver = true;
                }
            }, { passive: true });

            this.canvas.addEventListener('touchend', () => {
                this.isMouseOver = false;
                this.mouseX = null;
                this.mouseY = null;
            });

            // Visibility change - pause when tab is hidden
            document.addEventListener('visibilitychange', () => {
                if (document.hidden) {
                    this.stop();
                } else {
                    this.start();
                }
            });
        }

        drawConnections() {
            for (let i = 0; i < this.particles.length; i++) {
                for (let j = i + 1; j < this.particles.length; j++) {
                    const dx = this.particles[i].x - this.particles[j].x;
                    const dy = this.particles[i].y - this.particles[j].y;
                    const distance = Math.sqrt(dx * dx + dy * dy);

                    if (distance < CONFIG.connectionDistance) {
                        const opacity = (1 - distance / CONFIG.connectionDistance) * CONFIG.lineOpacity;
                        this.ctx.beginPath();
                        this.ctx.strokeStyle = `rgba(147, 51, 234, ${opacity})`;
                        this.ctx.lineWidth = 1;
                        this.ctx.moveTo(this.particles[i].x, this.particles[i].y);
                        this.ctx.lineTo(this.particles[j].x, this.particles[j].y);
                        this.ctx.stroke();
                    }
                }
            }

            // Draw connections to mouse cursor when hovering
            if (this.isMouseOver && this.mouseX !== null && this.mouseY !== null) {
                for (const particle of this.particles) {
                    const dx = this.mouseX - particle.x;
                    const dy = this.mouseY - particle.y;
                    const distance = Math.sqrt(dx * dx + dy * dy);

                    if (distance < CONFIG.mouseRadius) {
                        const opacity = (1 - distance / CONFIG.mouseRadius) * 0.3;
                        this.ctx.beginPath();
                        this.ctx.strokeStyle = `rgba(139, 92, 246, ${opacity})`;
                        this.ctx.lineWidth = 1.5;
                        this.ctx.moveTo(particle.x, particle.y);
                        this.ctx.lineTo(this.mouseX, this.mouseY);
                        this.ctx.stroke();
                    }
                }
            }
        }

        animate() {
            if (!this.isRunning) return;

            this.ctx.clearRect(0, 0, this.canvas.width, this.canvas.height);

            // Update and draw particles
            for (const particle of this.particles) {
                particle.update(this.mouseX, this.mouseY, this.isMouseOver);
                particle.draw(this.ctx);
            }

            // Draw connections between nearby particles
            this.drawConnections();

            this.animationId = requestAnimationFrame(() => this.animate());
        }

        start() {
            if (this.isRunning) return;
            this.isRunning = true;
            this.animate();
        }

        stop() {
            this.isRunning = false;
            if (this.animationId) {
                cancelAnimationFrame(this.animationId);
                this.animationId = null;
            }
        }

        destroy() {
            this.stop();
            this.particles = [];
        }
    }

    // Global initialization function
    window.MystiraParticles = {
        instance: null,
        
        init: function(canvasId) {
            // Clean up existing instance
            if (this.instance) {
                this.instance.destroy();
            }
            this.instance = new ParticleSystem(canvasId || 'particle-canvas');
        },

        destroy: function() {
            if (this.instance) {
                this.instance.destroy();
                this.instance = null;
            }
        },

        resize: function() {
            if (this.instance) {
                this.instance.resize();
                this.instance.createParticles();
            }
        }
    };
})();
