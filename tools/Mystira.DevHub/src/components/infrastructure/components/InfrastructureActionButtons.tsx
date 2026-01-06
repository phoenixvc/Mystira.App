import { DEPLOYMENT_METHODS, INFRASTRUCTURE_ACTIONS, type DeploymentMethod, type InfrastructureAction } from '../../../types';

interface InfrastructureActionButtonsProps {
  loading: boolean;
  hasValidated: boolean;
  hasPreviewed: boolean;
  currentAction: InfrastructureAction | null;
  whatIfChangesLength: number;
  deploymentMethod: DeploymentMethod;
  step1Collapsed: boolean;
  onAction: (action: InfrastructureAction) => Promise<void>;
  onShowDestroySelect: () => void;
  onStep1CollapsedChange: (collapsed: boolean) => void;
  hasSelectedTemplates: boolean;
}

export function InfrastructureActionButtons({
  loading,
  hasValidated,
  hasPreviewed,
  currentAction,
  whatIfChangesLength,
  deploymentMethod,
  step1Collapsed,
  onAction,
  onShowDestroySelect,
  onStep1CollapsedChange,
  hasSelectedTemplates,
}: InfrastructureActionButtonsProps) {
  return (
    <div id="step-2-infrastructure-actions" className="mb-4">
      <div className={`mb-4 ${hasSelectedTemplates ? 'sticky top-0 z-10 bg-white dark:bg-gray-900 pb-4 pt-2 -mt-2 border-b border-gray-200 dark:border-gray-700 mb-6 transition-all' : ''}`}>
        <div className="flex items-center justify-between mb-3">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
            Step 2: Infrastructure Actions
          </h3>
          {step1Collapsed && (
            <button
              onClick={() => onStep1CollapsedChange(false)}
              className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium"
            >
              ← Back to Step 1
            </button>
          )}
        </div>
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
          <button
            onClick={() => onAction(INFRASTRUCTURE_ACTIONS.VALIDATE)}
            disabled={loading}
            className="px-4 py-3 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
            title="Validate infrastructure templates"
          >
            <span>🔍</span>
            {currentAction === INFRASTRUCTURE_ACTIONS.VALIDATE && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            )}
            <span>Validate</span>
          </button>
          <button
            onClick={() => onAction(INFRASTRUCTURE_ACTIONS.PREVIEW)}
            disabled={loading || !hasValidated}
            className="px-4 py-3 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
            title={!hasValidated ? 'Please validate first' : 'Preview infrastructure changes'}
          >
            <span>👁️</span>
            {currentAction === INFRASTRUCTURE_ACTIONS.PREVIEW && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            )}
            <span>Preview</span>
          </button>
          <button
            onClick={() => onAction(INFRASTRUCTURE_ACTIONS.DEPLOY)}
            disabled={loading || !hasPreviewed}
            className="px-4 py-3 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
            title={!hasPreviewed ? 'Please preview first' : 'Deploy infrastructure'}
          >
            <span>🚀</span>
            {currentAction === INFRASTRUCTURE_ACTIONS.DEPLOY && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            )}
            <span>Deploy</span>
          </button>
          <button
            onClick={() => {
              if (whatIfChangesLength > 0 && deploymentMethod === DEPLOYMENT_METHODS.AZURE_CLI) {
                onShowDestroySelect();
              } else {
                onAction(INFRASTRUCTURE_ACTIONS.DESTROY);
              }
            }}
            disabled={loading}
            className="px-4 py-3 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
            title="Destroy infrastructure resources"
          >
            <span>💥</span>
            {currentAction === INFRASTRUCTURE_ACTIONS.DESTROY && (
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
            )}
            <span>Destroy</span>
          </button>
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-400 mt-3">
          Workflow: Validate → Preview → Deploy (or Destroy)
        </p>
      </div>
    </div>
  );
}

