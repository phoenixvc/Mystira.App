import { useState } from 'react';
import { ResourceCard } from './ResourceCard';

interface AzureResource {
  id: string;
  name: string;
  type: string;
  status: 'running' | 'stopped' | 'warning' | 'failed' | 'unknown';
  region: string;
  costToday?: number;
  properties?: Record<string, string>;
}

interface ResourceGridViewProps {
  resources: AzureResource[];
  compact: boolean;
  onDelete?: (resourceId: string, resourceName: string) => Promise<void>;
  deletingResource?: string | null;
}

export function ResourceGridView({
  resources,
  compact,
  onDelete,
  deletingResource,
}: ResourceGridViewProps) {
  return (
    <div
      className={`grid ${
        compact
          ? 'grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-2'
          : 'grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4'
      }`}
    >
      {resources.map((resource) => (
        <ResourceCard
          key={resource.id}
          resource={resource}
          compact={compact}
          onDelete={onDelete}
          deletingResource={deletingResource}
        />
      ))}
    </div>
  );
}

