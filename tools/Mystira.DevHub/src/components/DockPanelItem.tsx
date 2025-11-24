import { ReactNode } from 'react';

interface DockPanelItemProps {
  children: ReactNode;
  className?: string;
}

export function DockPanelItem({ children, className = '' }: DockPanelItemProps) {
  return (
    <div className={`p-4 ${className}`}>
      {children}
    </div>
  );
}

