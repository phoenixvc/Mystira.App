import type { WhatIfChange } from '../../../types';

export function parseWhatIfOutput(whatIfJson: string): WhatIfChange[] {
  try {
    const parsed = typeof whatIfJson === 'string' ? JSON.parse(whatIfJson) : whatIfJson;
    const changes: WhatIfChange[] = [];

    let changesArray: any[] = [];

    if (Array.isArray(parsed)) {
      changesArray = parsed;
    } else if (parsed.changes && Array.isArray(parsed.changes)) {
      changesArray = parsed.changes;
    } else if (parsed.resourceChanges && Array.isArray(parsed.resourceChanges)) {
      changesArray = parsed.resourceChanges;
    } else if (parsed.properties?.changes && Array.isArray(parsed.properties.changes)) {
      changesArray = parsed.properties.changes;
    } else if (parsed.properties?.resourceChanges && Array.isArray(parsed.properties.resourceChanges)) {
      changesArray = parsed.properties.resourceChanges;
    }

    if (changesArray.length > 0) {
      changesArray.forEach((change: any) => {
        if (!change.resourceId && !change.targetResource?.id) {
          return;
        }

        const resourceId = change.resourceId || change.targetResource?.id || '';
        const resourceIdParts = resourceId.split('/');
        const resourceName = resourceIdParts[resourceIdParts.length - 1] || resourceId;

        let resourceType = change.resourceType ||
                          change.targetResource?.type ||
                          change.resource?.type ||
                          (resourceIdParts.length >= 8 ? `${resourceIdParts[6]}/${resourceIdParts[7]}` : 'Unknown');

        if (resourceType && resourceType !== 'Unknown') {
          resourceType = resourceType.replace(/^microsoft\./i, 'Microsoft.');
        }

        let changeType: 'create' | 'modify' | 'delete' | 'noChange' = 'noChange';
        const azChangeType = (change.changeType || change.action || '').toLowerCase().trim();

        if (azChangeType === 'create' || azChangeType === 'deploy' || azChangeType === 'new') {
          changeType = 'create';
        } else if (azChangeType === 'modify' || azChangeType === 'update' || azChangeType === 'change') {
          changeType = 'modify';
        } else if (azChangeType === 'delete' || azChangeType === 'remove' || azChangeType === 'destroy') {
          changeType = 'delete';
        } else if (azChangeType === 'nochange' || azChangeType === 'ignore' || azChangeType === 'no-op') {
          changeType = 'noChange';
        }

        const propertyChanges: string[] = [];
        const delta = change.delta || change.changes || change.properties;

        if (delta) {
          if (Array.isArray(delta)) {
            delta.forEach((d: any) => {
              if (d.path || d.property) {
                const path = d.path || d.property;
                propertyChanges.push(`${path}: ${d.before || d.oldValue || 'null'} → ${d.after || d.newValue || 'null'}`);
              }
            });
          } else if (typeof delta === 'object') {
            Object.keys(delta).forEach((key: string) => {
              const deltaValue = delta[key];
              if (Array.isArray(deltaValue)) {
                deltaValue.forEach((d: any) => {
                  if (d.path || d.property) {
                    const path = d.path || d.property || key;
                    propertyChanges.push(`${path}: ${d.before || d.oldValue || 'null'} → ${d.after || d.newValue || 'null'}`);
                  }
                });
              } else if (deltaValue && typeof deltaValue === 'object') {
                propertyChanges.push(`${key}: ${JSON.stringify(deltaValue)}`);
              }
            });
          }
        }

        changes.push({
          resourceType: resourceType,
          resourceName: resourceName,
          changeType: changeType,
          changes: propertyChanges.length > 0 ? propertyChanges : undefined,
          selected: changeType !== 'noChange',
          resourceId: resourceId,
        });
      });
    }

    return changes;
  } catch (error) {
    console.error('Failed to parse what-if output:', error);
    return [];
  }
}

