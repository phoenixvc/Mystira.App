import { ReactNode } from 'react';

interface LiveRegionProps {
  children: ReactNode;
  role?: 'status' | 'alert' | 'log';
  'aria-live'?: 'polite' | 'assertive' | 'off';
  'aria-atomic'?: boolean;
}

/**
 * LiveRegion component for announcing dynamic content to screen readers
 * Use 'polite' for non-urgent updates (default)
 * Use 'assertive' for important/urgent updates
 */
function LiveRegion({
  children,
  role = 'status',
  'aria-live': ariaLive = 'polite',
  'aria-atomic': ariaAtomic = true,
}: LiveRegionProps) {
  return (
    <div
      role={role}
      aria-live={ariaLive}
      aria-atomic={ariaAtomic}
      className="sr-only"
    >
      {children}
    </div>
  );
}

export default LiveRegion;
