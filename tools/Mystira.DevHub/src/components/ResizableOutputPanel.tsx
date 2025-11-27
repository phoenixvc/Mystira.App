import { useCallback, useEffect, useRef, useState } from 'react';

export interface OutputPanelTab {
  id: string;
  label: string;
  icon?: string;
  content: React.ReactNode;
  badge?: number | string;
  badgeColor?: 'red' | 'yellow' | 'green' | 'blue' | 'gray';
}

interface ResizableOutputPanelProps {
  tabs: OutputPanelTab[];
  defaultHeight?: number;
  minHeight?: number;
  maxHeight?: number;
  defaultCollapsed?: boolean;
  storageKey?: string;
  className?: string;
}

export function ResizableOutputPanel({
  tabs,
  defaultHeight = 250,
  minHeight = 100,
  maxHeight = 600,
  defaultCollapsed = false,
  storageKey = 'outputPanelState',
  className = '',
}: ResizableOutputPanelProps) {
  const containerRef = useRef<HTMLDivElement>(null);
  const [activeTab, setActiveTab] = useState(tabs[0]?.id || '');
  const [isCollapsed, setIsCollapsed] = useState(() => {
    const saved = localStorage.getItem(`${storageKey}_collapsed`);
    return saved ? JSON.parse(saved) : defaultCollapsed;
  });
  const [height, setHeight] = useState(() => {
    const saved = localStorage.getItem(`${storageKey}_height`);
    return saved ? parseInt(saved, 10) : defaultHeight;
  });
  const [isDragging, setIsDragging] = useState(false);

  // Save state to localStorage
  useEffect(() => {
    localStorage.setItem(`${storageKey}_collapsed`, JSON.stringify(isCollapsed));
  }, [isCollapsed, storageKey]);

  useEffect(() => {
    localStorage.setItem(`${storageKey}_height`, String(height));
  }, [height, storageKey]);

  // Handle resize drag
  const handleMouseDown = useCallback((e: React.MouseEvent) => {
    e.preventDefault();
    setIsDragging(true);

    const startY = e.clientY;
    const startHeight = height;

    const handleMouseMove = (e: MouseEvent) => {
      const deltaY = startY - e.clientY;
      const newHeight = Math.max(minHeight, Math.min(maxHeight, startHeight + deltaY));
      setHeight(newHeight);
    };

    const handleMouseUp = () => {
      setIsDragging(false);
      document.removeEventListener('mousemove', handleMouseMove);
      document.removeEventListener('mouseup', handleMouseUp);
    };

    document.addEventListener('mousemove', handleMouseMove);
    document.addEventListener('mouseup', handleMouseUp);
  }, [height, minHeight, maxHeight]);

  const toggleCollapse = useCallback(() => {
    setIsCollapsed((prev: boolean) => !prev);
  }, []);

  const badgeColorClasses = {
    red: 'bg-red-500 text-white',
    yellow: 'bg-yellow-500 text-black',
    green: 'bg-green-500 text-white',
    blue: 'bg-blue-500 text-white',
    gray: 'bg-gray-500 text-white',
  };

  if (tabs.length === 0) return null;

  return (
    <div
      ref={containerRef}
      className={`flex flex-col border-t border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 ${className}`}
      style={{ height: isCollapsed ? 'auto' : `${height}px` }}
    >
      {/* Resize Handle */}
      {!isCollapsed && (
        <div
          className={`h-1 cursor-ns-resize bg-gray-200 dark:bg-gray-700 hover:bg-blue-400 dark:hover:bg-blue-500 transition-colors ${
            isDragging ? 'bg-blue-500 dark:bg-blue-400' : ''
          }`}
          onMouseDown={handleMouseDown}
          title="Drag to resize"
        />
      )}

      {/* Tab Bar */}
      <div className="flex items-center justify-between px-2 py-1 bg-gray-100 dark:bg-gray-700 border-b border-gray-200 dark:border-gray-600">
        <div className="flex items-center gap-1">
          {tabs.map((tab) => (
            <button
              key={tab.id}
              onClick={() => {
                setActiveTab(tab.id);
                if (isCollapsed) setIsCollapsed(false);
              }}
              className={`px-3 py-1 text-xs font-medium rounded-t transition-colors flex items-center gap-1.5 ${
                activeTab === tab.id && !isCollapsed
                  ? 'bg-white dark:bg-gray-800 text-blue-600 dark:text-blue-400 border-t border-l border-r border-gray-200 dark:border-gray-600 -mb-px'
                  : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-gray-200 dark:hover:bg-gray-600'
              }`}
            >
              {tab.icon && <span>{tab.icon}</span>}
              {tab.label}
              {tab.badge !== undefined && (
                <span
                  className={`px-1.5 py-0.5 text-[10px] rounded-full ${
                    badgeColorClasses[tab.badgeColor || 'gray']
                  }`}
                >
                  {tab.badge}
                </span>
              )}
            </button>
          ))}
        </div>
        <div className="flex items-center gap-1">
          <button
            onClick={toggleCollapse}
            className="p-1 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-500 dark:text-gray-400 text-xs"
            title={isCollapsed ? 'Expand panel' : 'Collapse panel'}
          >
            {isCollapsed ? '▲' : '▼'}
          </button>
        </div>
      </div>

      {/* Content */}
      {!isCollapsed && (
        <div className="flex-1 overflow-auto">
          {tabs.find((tab) => tab.id === activeTab)?.content}
        </div>
      )}
    </div>
  );
}

// Utility component for formatted output content
interface OutputContentProps {
  children: React.ReactNode;
  className?: string;
  monospace?: boolean;
}

export function OutputContent({ children, className = '', monospace = true }: OutputContentProps) {
  return (
    <div
      className={`p-3 text-xs overflow-auto ${
        monospace ? 'font-mono' : ''
      } ${className}`}
    >
      {children}
    </div>
  );
}

// Formatted error display component
interface ErrorDisplayProps {
  error: string | null;
  details?: Record<string, unknown> | null;
  onCopy?: () => void;
}

export function ErrorDisplay({ error, details, onCopy }: ErrorDisplayProps) {
  const [showDetails, setShowDetails] = useState(false);
  const [copied, setCopied] = useState(false);

  const handleCopy = async () => {
    const text = details
      ? `${error}\n\nDetails:\n${JSON.stringify(details, null, 2)}`
      : error || '';
    await navigator.clipboard.writeText(text);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
    onCopy?.();
  };

  if (!error) return null;

  return (
    <div className="p-3 text-xs">
      <div className="flex items-start justify-between gap-2 mb-2">
        <div className="flex items-center gap-2 text-red-600 dark:text-red-400 font-semibold">
          <span>✕</span>
          <span>Error</span>
        </div>
        <button
          onClick={handleCopy}
          className="px-2 py-0.5 text-[10px] bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 rounded text-gray-600 dark:text-gray-400 transition-colors"
        >
          {copied ? '✓ Copied' : 'Copy'}
        </button>
      </div>

      {/* Error message with word wrap */}
      <pre className="whitespace-pre-wrap break-words text-red-700 dark:text-red-300 bg-red-50 dark:bg-red-900/30 p-3 rounded border border-red-200 dark:border-red-800 overflow-x-auto">
        {error}
      </pre>

      {/* Details section */}
      {details && (
        <div className="mt-3">
          <button
            onClick={() => setShowDetails(!showDetails)}
            className="flex items-center gap-1 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
          >
            <span className="text-[10px]">{showDetails ? '▼' : '▶'}</span>
            <span>View Details</span>
          </button>
          {showDetails && (
            <pre className="mt-2 whitespace-pre-wrap break-words text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900 p-3 rounded border border-gray-200 dark:border-gray-700 overflow-x-auto max-h-64">
              {JSON.stringify(details, null, 2)}
            </pre>
          )}
        </div>
      )}
    </div>
  );
}

// Success display component
interface SuccessDisplayProps {
  message: string | null;
  details?: Record<string, unknown> | null;
}

export function SuccessDisplay({ message, details }: SuccessDisplayProps) {
  const [showDetails, setShowDetails] = useState(false);

  if (!message) return null;

  return (
    <div className="p-3 text-xs">
      <div className="flex items-center gap-2 text-green-600 dark:text-green-400 font-semibold mb-2">
        <span>✓</span>
        <span>Success</span>
      </div>

      <p className="text-green-700 dark:text-green-300 bg-green-50 dark:bg-green-900/30 p-3 rounded border border-green-200 dark:border-green-800">
        {message}
      </p>

      {details && (
        <div className="mt-3">
          <button
            onClick={() => setShowDetails(!showDetails)}
            className="flex items-center gap-1 text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-300 transition-colors"
          >
            <span className="text-[10px]">{showDetails ? '▼' : '▶'}</span>
            <span>View Details</span>
          </button>
          {showDetails && (
            <pre className="mt-2 whitespace-pre-wrap break-words text-gray-700 dark:text-gray-300 bg-gray-50 dark:bg-gray-900 p-3 rounded border border-gray-200 dark:border-gray-700 overflow-x-auto max-h-64">
              {JSON.stringify(details, null, 2)}
            </pre>
          )}
        </div>
      )}
    </div>
  );
}
