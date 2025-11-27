import { useEffect, useRef, useState } from 'react';

export function useServiceCardResize(serviceName: string) {
  const [logHeight, setLogHeight] = useState(() => {
    const saved = localStorage.getItem(`service-${serviceName}-log-height`);
    return saved ? parseInt(saved, 10) : 384;
  });
  const [isResizing, setIsResizing] = useState(false);
  const resizeHandleRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!isResizing) return;

    const handleMouseMove = (e: MouseEvent) => {
      if (!resizeHandleRef.current) return;
      const rect = resizeHandleRef.current.getBoundingClientRect();
      const newHeight = window.innerHeight - e.clientY;
      const minHeight = 100;
      const maxHeight = window.innerHeight * 0.8;
      const clampedHeight = Math.max(minHeight, Math.min(maxHeight, newHeight));
      setLogHeight(clampedHeight);
      localStorage.setItem(`service-${serviceName}-log-height`, clampedHeight.toString());
    };

    const handleMouseUp = () => {
      setIsResizing(false);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
    document.body.style.cursor = 'ns-resize';
    document.body.style.userSelect = 'none';

    return () => {
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
      document.body.style.cursor = '';
      document.body.style.userSelect = '';
    };
  }, [isResizing, serviceName]);

  const handleResizeStart = (e: React.MouseEvent) => {
    e.preventDefault();
    setIsResizing(true);
  };

  return {
    logHeight,
    isResizing,
    resizeHandleRef,
    handleResizeStart,
  };
}

