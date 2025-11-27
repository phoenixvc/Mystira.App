import type { WhatIfChange } from '../../../types';

interface WhatIfWarningsProps {
  deleteCount: number;
  showSelection: boolean;
  localChanges: WhatIfChange[];
  compact: boolean;
}

export function WhatIfWarnings({ deleteCount, showSelection, localChanges, compact }: WhatIfWarningsProps) {
  const selectedModules = new Set(
    localChanges
      .filter((c) => c.selected !== false)
      .map((c) => {
        const type = (c.resourceType || '').toLowerCase();
        if (type.includes('web/sites') || type.includes('appservice') || type.includes('web/serverfarms'))
          return 'appservice';
        if (type.includes('documentdb') || type.includes('cosmos')) return 'cosmos';
        if (type.includes('storage') && type.includes('account')) return 'storage';
        return null;
      })
      .filter(Boolean)
  );

  const hasAppService = selectedModules.has('appservice');
  const hasCosmos = selectedModules.has('cosmos');
  const hasStorage = selectedModules.has('storage');
  const missingDeps: string[] = [];

  if (hasAppService) {
    if (!hasCosmos) missingDeps.push('Cosmos DB');
    if (!hasStorage) missingDeps.push('Storage Account');
  }

  return (
    <>
      {deleteCount > 0 && (
        <div
          className={`bg-red-50 dark:bg-red-900/30 border-t border-red-200 dark:border-red-800 ${compact ? 'p-2' : 'p-4'}`}
        >
          <div className="flex items-start">
            <span className={compact ? 'text-sm mr-2' : 'text-2xl mr-3'}>⚠️</span>
            <div>
              <div className={`font-semibold text-red-900 dark:text-red-300 ${compact ? 'text-xs' : 'mb-1'}`}>
                Warning: Destructive Changes
              </div>
              <div className={`${compact ? 'text-[10px]' : 'text-sm'} text-red-800 dark:text-red-400`}>
                {deleteCount} resource{deleteCount > 1 ? 's' : ''} will be deleted. This action cannot be undone.
              </div>
            </div>
          </div>
        </div>
      )}

      {showSelection && missingDeps.length > 0 && (
        <div
          className={`bg-yellow-50 dark:bg-yellow-900/30 border-t border-yellow-200 dark:border-yellow-800 ${compact ? 'p-2' : 'p-4'}`}
        >
          <div className="flex items-start">
            <span className={compact ? 'text-sm mr-2' : 'text-2xl mr-3'}>⚠️</span>
            <div>
              <div className={`font-semibold text-yellow-900 dark:text-yellow-300 ${compact ? 'text-xs' : 'mb-1'}`}>
                Dependency Warning
              </div>
              <div className={`${compact ? 'text-[10px]' : 'text-sm'} text-yellow-800 dark:text-yellow-400`}>
                App Service needs {missingDeps.join(' and ')} selected.
              </div>
            </div>
          </div>
        </div>
      )}
    </>
  );
}

