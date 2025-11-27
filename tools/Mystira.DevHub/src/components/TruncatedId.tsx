import { useState } from 'react';

interface TruncatedIdProps {
  id: string;
  /** Number of characters to show at start */
  showStart?: number;
  /** Number of characters to show at end */
  showEnd?: number;
  /** Show full ID on hover */
  showFullOnHover?: boolean;
  /** Additional CSS classes */
  className?: string;
  /** Text size class (default: text-xs) */
  size?: 'xs' | 'sm' | 'base';
}

/**
 * Component for displaying truncated GUIDs/IDs with copy functionality.
 * Shows abbreviated version by default, with click-to-copy and hover preview.
 */
export function TruncatedId({
  id,
  showStart = 8,
  showEnd = 4,
  showFullOnHover = true,
  className = '',
  size = 'xs',
}: TruncatedIdProps) {
  const [copied, setCopied] = useState(false);
  const [showFull, setShowFull] = useState(false);

  if (!id) return null;

  // Don't truncate if ID is short enough
  const shouldTruncate = id.length > showStart + showEnd + 3;
  const truncatedId = shouldTruncate
    ? `${id.slice(0, showStart)}...${id.slice(-showEnd)}`
    : id;

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(id);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch (error) {
      console.error('Failed to copy ID:', error);
    }
  };

  const sizeClass = {
    xs: 'text-[10px]',
    sm: 'text-xs',
    base: 'text-sm',
  }[size];

  return (
    <span
      className={`inline-flex items-center gap-1 font-mono ${className}`}
      onMouseEnter={() => showFullOnHover && setShowFull(true)}
      onMouseLeave={() => setShowFull(false)}
    >
      <code
        className={`px-1.5 py-0.5 bg-gray-100 dark:bg-gray-800 rounded ${sizeClass} text-gray-700 dark:text-gray-300 cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors`}
        onClick={handleCopy}
        title={`${id}\n\nClick to copy`}
      >
        {showFull ? id : truncatedId}
      </code>
      {copied && (
        <span className={`${sizeClass} text-green-600 dark:text-green-400 font-semibold`}>
          ✓
        </span>
      )}
    </span>
  );
}

/**
 * Utility function to truncate GUIDs/IDs for display
 */
export function truncateId(id: string, showStart = 8, showEnd = 4): string {
  if (!id || id.length <= showStart + showEnd + 3) return id;
  return `${id.slice(0, showStart)}...${id.slice(-showEnd)}`;
}

/**
 * Component for displaying Azure Resource IDs in a compact format
 * Extracts and shows resource group and name
 */
interface AzureResourceIdProps {
  resourceId: string;
  className?: string;
}

export function AzureResourceId({ resourceId, className = '' }: AzureResourceIdProps) {
  const [copied, setCopied] = useState(false);

  if (!resourceId) return null;

  // Parse Azure Resource ID: /subscriptions/.../resourceGroups/.../providers/.../resourceType/resourceName
  const parts = resourceId.split('/');
  const rgIndex = parts.findIndex(p => p.toLowerCase() === 'resourcegroups');
  const resourceGroup = rgIndex >= 0 && parts[rgIndex + 1] ? parts[rgIndex + 1] : null;
  const resourceName = parts[parts.length - 1];

  const handleCopy = async () => {
    try {
      await navigator.clipboard.writeText(resourceId);
      setCopied(true);
      setTimeout(() => setCopied(false), 1500);
    } catch (error) {
      console.error('Failed to copy Resource ID:', error);
    }
  };

  return (
    <span
      className={`inline-flex items-center gap-1 font-mono ${className}`}
      onClick={handleCopy}
      title={`${resourceId}\n\nClick to copy full resource ID`}
    >
      {resourceGroup && (
        <>
          <code className="text-[10px] px-1 py-0.5 bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300 rounded cursor-pointer hover:bg-blue-200 dark:hover:bg-blue-900/60 transition-colors">
            {resourceGroup}
          </code>
          <span className="text-gray-400">/</span>
        </>
      )}
      <code className="text-[10px] px-1 py-0.5 bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 rounded cursor-pointer hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors">
        {resourceName}
      </code>
      {copied && (
        <span className="text-[10px] text-green-600 dark:text-green-400 font-semibold">
          ✓
        </span>
      )}
    </span>
  );
}

export default TruncatedId;
