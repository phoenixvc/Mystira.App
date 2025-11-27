import { useCallback, useMemo, useState } from 'react';

interface JsonViewerProps {
  data: unknown;
  collapsed?: boolean;
  maxHeight?: string;
  className?: string;
  showCopyButton?: boolean;
  compact?: boolean;
}

// Syntax highlighting for JSON
function highlightJson(json: string): JSX.Element[] {
  const elements: JSX.Element[] = [];
  let key = 0;

  // Regex patterns for JSON tokens
  const patterns = [
    { regex: /"[^"]*"(?=\s*:)/g, className: 'text-purple-600 dark:text-purple-400' }, // keys
    { regex: /"[^"]*"(?!\s*:)/g, className: 'text-green-600 dark:text-green-400' }, // string values
    { regex: /\b(true|false)\b/g, className: 'text-blue-600 dark:text-blue-400 font-semibold' }, // booleans
    { regex: /\bnull\b/g, className: 'text-gray-500 dark:text-gray-400 italic' }, // null
    { regex: /\b-?\d+\.?\d*(?:[eE][+-]?\d+)?\b/g, className: 'text-orange-600 dark:text-orange-400' }, // numbers
    { regex: /[{}\[\]]/g, className: 'text-gray-700 dark:text-gray-300 font-bold' }, // brackets
    { regex: /[:,]/g, className: 'text-gray-500 dark:text-gray-400' }, // punctuation
  ];

  // Split by newlines first to preserve formatting
  const lines = json.split('\n');

  lines.forEach((line, lineIndex) => {
    // Create a map of positions to their token type
    const tokens: Array<{ start: number; end: number; className: string }> = [];

    patterns.forEach(({ regex, className }) => {
      let match;
      const regexCopy = new RegExp(regex.source, 'g');
      while ((match = regexCopy.exec(line)) !== null) {
        tokens.push({
          start: match.index,
          end: match.index + match[0].length,
          className,
        });
      }
    });

    // Sort by position and remove overlaps
    tokens.sort((a, b) => a.start - b.start);
    const filteredTokens: typeof tokens = [];
    for (const token of tokens) {
      const last = filteredTokens[filteredTokens.length - 1];
      if (!last || token.start >= last.end) {
        filteredTokens.push(token);
      }
    }

    // Build elements for this line
    let lastEnd = 0;
    const lineElements: JSX.Element[] = [];

    filteredTokens.forEach((token) => {
      // Add plain text before this token
      if (token.start > lastEnd) {
        lineElements.push(
          <span key={key++}>{line.slice(lastEnd, token.start)}</span>
        );
      }
      // Add highlighted token
      lineElements.push(
        <span key={key++} className={token.className}>
          {line.slice(token.start, token.end)}
        </span>
      );
      lastEnd = token.end;
    });

    // Add remaining text
    if (lastEnd < line.length) {
      lineElements.push(<span key={key++}>{line.slice(lastEnd)}</span>);
    }

    // Add the line with newline
    elements.push(
      <div key={`line-${lineIndex}`} className="whitespace-pre">
        {lineElements.length > 0 ? lineElements : '\u00A0'}
      </div>
    );
  });

  return elements;
}

export function JsonViewer({
  data,
  collapsed = false,
  maxHeight = '400px',
  className = '',
  showCopyButton = true,
  compact = false,
}: JsonViewerProps) {
  const [isCollapsed, setIsCollapsed] = useState(collapsed);
  const [copied, setCopied] = useState(false);

  const jsonString = useMemo(() => {
    try {
      return JSON.stringify(data, null, 2);
    } catch {
      return String(data);
    }
  }, [data]);

  const highlightedElements = useMemo(() => {
    return highlightJson(jsonString);
  }, [jsonString]);

  const handleCopy = useCallback(async () => {
    try {
      await navigator.clipboard.writeText(jsonString);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    } catch (error) {
      console.error('Failed to copy:', error);
    }
  }, [jsonString]);

  const lineCount = jsonString.split('\n').length;

  if (data === null || data === undefined) {
    return (
      <div className={`text-gray-500 dark:text-gray-400 italic text-xs ${className}`}>
        null
      </div>
    );
  }

  return (
    <div className={`rounded border border-gray-200 dark:border-gray-700 overflow-hidden ${className}`}>
      {/* Header */}
      <div className="flex items-center justify-between px-3 py-1.5 bg-gray-100 dark:bg-gray-800 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center gap-2">
          <button
            onClick={() => setIsCollapsed(!isCollapsed)}
            className="text-xs text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 transition-colors"
            title={isCollapsed ? 'Expand' : 'Collapse'}
          >
            {isCollapsed ? '▶' : '▼'}
          </button>
          <span className="text-xs text-gray-500 dark:text-gray-400">
            {typeof data === 'object' && data !== null
              ? Array.isArray(data)
                ? `Array[${data.length}]`
                : `Object{${Object.keys(data).length}}`
              : typeof data}
          </span>
          <span className="text-[10px] text-gray-400 dark:text-gray-500">
            {lineCount} lines
          </span>
        </div>
        {showCopyButton && (
          <button
            onClick={handleCopy}
            className={`px-2 py-0.5 text-[10px] rounded transition-colors ${
              copied
                ? 'bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400'
                : 'bg-gray-200 dark:bg-gray-700 text-gray-600 dark:text-gray-400 hover:bg-gray-300 dark:hover:bg-gray-600'
            }`}
          >
            {copied ? '✓ Copied' : 'Copy'}
          </button>
        )}
      </div>

      {/* Content */}
      {!isCollapsed && (
        <div
          className={`overflow-auto bg-gray-50 dark:bg-gray-900 ${compact ? 'p-2' : 'p-3'}`}
          style={{ maxHeight }}
        >
          <code className={`block font-mono ${compact ? 'text-[10px]' : 'text-xs'} leading-relaxed`}>
            {highlightedElements}
          </code>
        </div>
      )}
    </div>
  );
}

// Inline JSON value display (for compact single-line values)
interface InlineJsonProps {
  value: unknown;
  className?: string;
}

export function InlineJson({ value, className = '' }: InlineJsonProps) {
  const display = useMemo(() => {
    if (value === null) return <span className="text-gray-500 italic">null</span>;
    if (value === undefined) return <span className="text-gray-500 italic">undefined</span>;
    if (typeof value === 'boolean') {
      return <span className="text-blue-600 dark:text-blue-400 font-semibold">{String(value)}</span>;
    }
    if (typeof value === 'number') {
      return <span className="text-orange-600 dark:text-orange-400">{value}</span>;
    }
    if (typeof value === 'string') {
      return <span className="text-green-600 dark:text-green-400">"{value}"</span>;
    }
    if (Array.isArray(value)) {
      return <span className="text-gray-600 dark:text-gray-400">[Array({value.length})]</span>;
    }
    if (typeof value === 'object') {
      return <span className="text-gray-600 dark:text-gray-400">{'{Object}'}</span>;
    }
    return <span>{String(value)}</span>;
  }, [value]);

  return <span className={`font-mono text-xs ${className}`}>{display}</span>;
}

export default JsonViewer;
