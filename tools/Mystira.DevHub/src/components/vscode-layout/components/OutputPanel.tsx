import { useEffect, useRef, useState } from 'react';

interface OutputPanelProps {
  children: React.ReactNode;
  autoScroll?: boolean;
  className?: string;
}

export function OutputPanel({ children, autoScroll = true, className = '' }: OutputPanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [isAtBottom, setIsAtBottom] = useState(true);

  useEffect(() => {
    if (autoScroll && isAtBottom && containerRef.current) {
      containerRef.current.scrollTop = containerRef.current.scrollHeight;
    }
  }, [children, autoScroll, isAtBottom]);

  const handleScroll = () => {
    if (!containerRef.current) return;
    const { scrollTop, scrollHeight, clientHeight } = containerRef.current;
    setIsAtBottom(scrollTop + clientHeight >= scrollHeight - 10);
  };

  return (
    <div
      ref={containerRef}
      onScroll={handleScroll}
      className={`h-full overflow-auto font-mono text-xs bg-gray-900 ${className}`}
    >
      {children}
    </div>
  );
}

