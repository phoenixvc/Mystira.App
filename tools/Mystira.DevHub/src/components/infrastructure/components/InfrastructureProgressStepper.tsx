import type { TemplateConfig } from '../../../types';

interface InfrastructureProgressStepperProps {
  templates: TemplateConfig[];
  hasValidated: boolean;
  hasPreviewed: boolean;
  showStep2: boolean;
  step1Collapsed: boolean;
  onStep1CollapsedChange: (collapsed: boolean) => void;
}

export function InfrastructureProgressStepper({
  templates,
  hasValidated,
  hasPreviewed,
  showStep2,
  step1Collapsed,
  onStep1CollapsedChange,
}: InfrastructureProgressStepperProps) {
  const hasSelectedTemplates = templates.some(t => t.selected);

  return (
    <div className="mb-6 px-4 py-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-4 flex-1">
          {/* Step 1 */}
          <div className="flex items-center gap-2">
            <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
              hasSelectedTemplates 
                ? 'bg-blue-600 dark:bg-blue-500 text-white' 
                : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
            }`}>
              {hasSelectedTemplates ? '✓' : '1'}
            </div>
            <span className={`text-sm font-medium ${
              hasSelectedTemplates
                ? 'text-blue-600 dark:text-blue-400'
                : 'text-gray-600 dark:text-gray-400'
            }`}>
              Plan Deployment
            </span>
          </div>
          
          {/* Connector - only show when Step 2 is visible */}
          {showStep2 && (
            <>
              <div className={`flex-1 h-0.5 ${
                hasSelectedTemplates
                  ? 'bg-blue-600 dark:bg-blue-500'
                  : 'bg-gray-300 dark:bg-gray-600'
              }`} />
              
              {/* Step 2 */}
              <div className="flex items-center gap-2">
                <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
                  hasValidated 
                    ? 'bg-purple-600 dark:bg-purple-500 text-white' 
                    : hasSelectedTemplates
                    ? 'bg-blue-600 dark:bg-blue-500 text-white'
                    : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
                }`}>
                  {hasPreviewed ? '✓' : '2'}
                </div>
                <span className={`text-sm font-medium ${
                  hasValidated || hasSelectedTemplates
                    ? 'text-blue-600 dark:text-blue-400'
                    : 'text-gray-600 dark:text-gray-400'
                }`}>
                  Infrastructure Actions
                </span>
              </div>
            </>
          )}
        </div>
        
        {/* Collapse Toggle */}
        {showStep2 && hasSelectedTemplates && (
          <button
            onClick={() => onStep1CollapsedChange(!step1Collapsed)}
            className="px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 rounded-md border border-gray-300 dark:border-gray-600 transition-colors flex items-center gap-1.5"
            title={step1Collapsed ? 'Expand Step 1' : 'Collapse Step 1'}
          >
            {step1Collapsed ? (
              <>
                <span>▼</span>
                <span>Show Step 1</span>
              </>
            ) : (
              <>
                <span>▲</span>
                <span>Hide Step 1</span>
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
}

