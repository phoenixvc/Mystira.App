import { useState } from 'react';

interface DestroyButtonProps {
  onClick: () => void;
  disabled?: boolean;
  loading?: boolean;
}

export function DestroyButton({ onClick, disabled, loading }: DestroyButtonProps) {
  const [isHovered, setIsHovered] = useState(false);

  // Calculate particle positions and create CSS variables
  const outerParticles = [...Array(16)].map((_, i) => {
    const angle = (i * 22.5) * (Math.PI / 180);
    return {
      angle,
      x: 100 + Math.cos(angle) * 40,
      y: 100 + Math.sin(angle) * 40,
      particleX: Math.cos(angle) * 30,
      particleY: Math.sin(angle) * 30,
    };
  });

  const innerParticles = [...Array(8)].map((_, i) => {
    const angle = (i * 45) * (Math.PI / 180);
    return {
      angle,
      x: 100 + Math.cos(angle) * 25,
      y: 100 + Math.sin(angle) * 25,
      particleX: Math.cos(angle) * 20,
      particleY: Math.sin(angle) * 20,
    };
  });

  const sparkParticles = [...Array(24)].map((_, i) => {
    const angle = (i * 15) * (Math.PI / 180);
    return {
      angle,
      x: 100 + Math.cos(angle) * 35,
      y: 100 + Math.sin(angle) * 35,
      particleX: Math.cos(angle) * 25,
      particleY: Math.sin(angle) * 25,
    };
  });

  // Create CSS variables string for SVG
  const svgStyle = {
    ...outerParticles.reduce((acc, p, i) => {
      acc[`--particle-outer-${i}-x`] = `${p.particleX}px`;
      acc[`--particle-outer-${i}-y`] = `${p.particleY}px`;
      return acc;
    }, {} as Record<string, string>),
    ...innerParticles.reduce((acc, p, i) => {
      acc[`--particle-inner-${i}-x`] = `${p.particleX}px`;
      acc[`--particle-inner-${i}-y`] = `${p.particleY}px`;
      return acc;
    }, {} as Record<string, string>),
    ...sparkParticles.reduce((acc, p, i) => {
      acc[`--particle-spark-${i}-x`] = `${p.particleX}px`;
      acc[`--particle-spark-${i}-y`] = `${p.particleY}px`;
      return acc;
    }, {} as Record<string, string>),
  } as React.CSSProperties;

  return (
    <button
      onClick={onClick}
      onMouseEnter={() => setIsHovered(true)}
      onMouseLeave={() => setIsHovered(false)}
      disabled={disabled || loading}
      className="destroy-button-epic flex flex-col items-center p-6 bg-white dark:bg-gray-800 border-2 border-red-500 dark:border-red-700 rounded-lg transition-all disabled:opacity-50 disabled:cursor-not-allowed relative overflow-visible"
    >
      {/* Explosion SVG Background */}
      <div className="absolute inset-0 overflow-visible pointer-events-none">
        <svg
          className="absolute inset-0 w-full h-full"
          viewBox="0 0 200 200"
          xmlns="http://www.w3.org/2000/svg"
          style={svgStyle}
        >
          {/* Shockwave rings */}
          <circle
            className="shockwave-1"
            cx="100"
            cy="100"
            r="50"
            fill="none"
            stroke="url(#redGradient)"
            strokeWidth="3"
            opacity="0.6"
          />
          <circle
            className="shockwave-2"
            cx="100"
            cy="100"
            r="50"
            fill="none"
            stroke="url(#redGradient)"
            strokeWidth="2"
            opacity="0.4"
          />
          <circle
            className="shockwave-3"
            cx="100"
            cy="100"
            r="50"
            fill="none"
            stroke="url(#redGradient)"
            strokeWidth="1"
            opacity="0.2"
          />

          {/* Explosion particles - outer ring */}
          {outerParticles.map((particle, i) => (
            <circle
              key={`outer-${i}`}
              className={`explosion-particle particle-outer-${i}`}
              cx={particle.x}
              cy={particle.y}
              r="5"
              fill="url(#fireGradient)"
            />
          ))}
          {/* Explosion particles - inner ring */}
          {innerParticles.map((particle, i) => (
            <circle
              key={`inner-${i}`}
              className={`explosion-particle particle-inner-${i}`}
              cx={particle.x}
              cy={particle.y}
              r="6"
              fill="url(#coreGradient)"
            />
          ))}
          {/* Spark particles */}
          {sparkParticles.map((particle, i) => (
            <circle
              key={`spark-${i}`}
              className={`explosion-particle particle-spark-${i}`}
              cx={particle.x}
              cy={particle.y}
              r="2"
              fill="#fbbf24"
              opacity="0.9"
            />
          ))}

          {/* Fire flames - multiple layers */}
          <path
            className="flame-1"
            d="M 85 115 Q 80 105 85 85 Q 90 75 100 85 Q 110 75 115 85 Q 120 105 115 115 Z"
            fill="url(#fireGradient)"
            opacity="0.9"
          />
          <path
            className="flame-2"
            d="M 90 110 Q 87 100 90 90 Q 93 82 100 90 Q 107 82 110 90 Q 113 100 110 110 Z"
            fill="url(#fireGradient)"
            opacity="0.95"
          />
          <path
            className="flame-3"
            d="M 95 105 Q 93 98 95 92 Q 97 87 100 92 Q 103 87 105 92 Q 107 98 105 105 Z"
            fill="url(#fireGradient)"
            opacity="1"
          />
          {/* Additional fire bursts */}
          <path
            className="flame-burst-1"
            d="M 100 120 L 95 130 L 100 125 L 105 130 Z"
            fill="#f59e0b"
            opacity="0.8"
          />
          <path
            className="flame-burst-2"
            d="M 100 120 L 92 128 L 100 123 L 108 128 Z"
            fill="#fbbf24"
            opacity="0.7"
          />

          {/* Central explosion core */}
          <circle
            className="explosion-core"
            cx="100"
            cy="100"
            r="15"
            fill="url(#coreGradient)"
            filter="url(#glow)"
          />

          {/* Gradients */}
          <defs>
            <linearGradient id="redGradient" x1="0%" y1="0%" x2="100%" y2="100%">
              <stop offset="0%" stopColor="#ef4444" stopOpacity="1" />
              <stop offset="100%" stopColor="#dc2626" stopOpacity="0.3" />
            </linearGradient>
            <radialGradient id="fireGradient" cx="50%" cy="50%">
              <stop offset="0%" stopColor="#fbbf24" stopOpacity="1" />
              <stop offset="50%" stopColor="#f59e0b" stopOpacity="0.9" />
              <stop offset="100%" stopColor="#ef4444" stopOpacity="0.7" />
            </radialGradient>
            <radialGradient id="coreGradient" cx="50%" cy="50%">
              <stop offset="0%" stopColor="#ffffff" stopOpacity="1" />
              <stop offset="30%" stopColor="#fbbf24" stopOpacity="1" />
              <stop offset="70%" stopColor="#f59e0b" stopOpacity="1" />
              <stop offset="100%" stopColor="#ef4444" stopOpacity="1" />
            </radialGradient>
            <filter id="glow">
              <feGaussianBlur stdDeviation="3" result="coloredBlur" />
              <feMerge>
                <feMergeNode in="coloredBlur" />
                <feMergeNode in="SourceGraphic" />
              </feMerge>
            </filter>
          </defs>
        </svg>
      </div>

      {/* Button Content */}
      <div className="relative z-10 flex flex-col items-center">
        <div className="destroy-icon-epic text-5xl mb-2 transform transition-transform">
          💥
        </div>
        <div className="text-lg font-bold text-red-600 dark:text-red-400">DESTROY</div>
        <div className="text-xs font-semibold text-red-500 dark:text-red-500 text-center mt-1 uppercase tracking-wider">
          Delete All Resources
        </div>
      </div>

      {/* Warning overlay on hover */}
      {isHovered && (
        <div className="absolute inset-0 bg-red-600 dark:bg-red-700 opacity-20 rounded-lg animate-pulse-fast"></div>
      )}
    </button>
  );
}

