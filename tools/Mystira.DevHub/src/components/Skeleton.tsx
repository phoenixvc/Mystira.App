import React from 'react';

interface SkeletonProps {
  className?: string;
  width?: string | number;
  height?: string | number;
  circle?: boolean;
  count?: number;
}

/**
 * Skeleton component for loading states
 * Provides visual feedback while content is loading
 */
function Skeleton({
  className = '',
  width,
  height,
  circle = false,
  count = 1,
}: SkeletonProps) {
  const baseClasses = 'animate-pulse bg-gray-200 rounded';
  const circleClasses = circle ? 'rounded-full' : '';

  const style: React.CSSProperties = {
    width: width || '100%',
    height: height || '1rem',
  };

  const skeletonElement = (
    <div
      className={`${baseClasses} ${circleClasses} ${className}`}
      style={style}
      aria-hidden="true"
    />
  );

  if (count === 1) {
    return skeletonElement;
  }

  return (
    <>
      {Array.from({ length: count }).map((_, index) => (
        <React.Fragment key={index}>{skeletonElement}</React.Fragment>
      ))}
    </>
  );
}

/**
 * Card skeleton for loading card-based layouts
 */
export function CardSkeleton({ className = '' }: { className?: string }) {
  return (
    <div className={`border rounded-lg p-4 ${className}`}>
      <div className="flex items-center mb-3">
        <Skeleton circle width={40} height={40} className="mr-3" />
        <div className="flex-1">
          <Skeleton width="60%" height={16} className="mb-2" />
          <Skeleton width="40%" height={12} />
        </div>
      </div>
      <Skeleton count={3} height={12} className="mb-2" />
    </div>
  );
}

/**
 * List skeleton for loading list items
 */
export function ListSkeleton({ count = 5 }: { count?: number }) {
  return (
    <div className="space-y-3">
      {Array.from({ length: count }).map((_, index) => (
        <div key={index} className="flex items-center p-3 border rounded">
          <Skeleton circle width={32} height={32} className="mr-3" />
          <div className="flex-1">
            <Skeleton width="70%" height={14} className="mb-2" />
            <Skeleton width="50%" height={12} />
          </div>
        </div>
      ))}
    </div>
  );
}

/**
 * Table skeleton for loading table data
 */
export function TableSkeleton({ rows = 5, columns = 4 }: { rows?: number; columns?: number }) {
  return (
    <div className="border rounded-lg overflow-hidden">
      {/* Header */}
      <div className="bg-gray-50 p-4 border-b">
        <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
          {Array.from({ length: columns }).map((_, index) => (
            <Skeleton key={index} height={16} />
          ))}
        </div>
      </div>
      {/* Rows */}
      <div>
        {Array.from({ length: rows }).map((_, rowIndex) => (
          <div key={rowIndex} className="p-4 border-b last:border-b-0">
            <div className="grid gap-4" style={{ gridTemplateColumns: `repeat(${columns}, 1fr)` }}>
              {Array.from({ length: columns }).map((_, colIndex) => (
                <Skeleton key={colIndex} height={14} />
              ))}
            </div>
          </div>
        ))}
      </div>
    </div>
  );
}

export default Skeleton;
