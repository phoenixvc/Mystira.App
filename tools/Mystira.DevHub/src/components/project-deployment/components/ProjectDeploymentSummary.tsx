import type { TemplateConfig } from '../../../types';

interface ProjectDeploymentSummaryProps {
  selectedTemplates: TemplateConfig[];
  readyToProceed: boolean;
  infrastructureLoading: boolean;
  loadingStatus: boolean;
  onProceedToStep2?: () => void;
}

export function ProjectDeploymentSummary({
  selectedTemplates,
  readyToProceed,
  infrastructureLoading,
  loadingStatus,
  onProceedToStep2,
}: ProjectDeploymentSummaryProps) {
  const handleContinue = () => {
    if (!readyToProceed) return;

    onProceedToStep2?.();

    setTimeout(() => {
      const step2Element = document.getElementById('step-2-infrastructure-actions');
      if (step2Element) {
        requestAnimationFrame(() => {
          const elementPosition = step2Element.getBoundingClientRect().top;
          const offsetPosition = elementPosition + window.pageYOffset - 20;

          window.scrollTo({
            top: offsetPosition,
            behavior: 'smooth'
          });

          step2Element.classList.add('ring-2', 'ring-blue-500', 'rounded-lg');
          setTimeout(() => {
            step2Element.classList.remove('ring-2', 'ring-blue-500', 'rounded-lg');
          }, 2000);
        });
      }
    }, 100);
  };

  const isLoading = infrastructureLoading || loadingStatus;

  // Empty state with guidance
  if (selectedTemplates.length === 0 && !isLoading) {
    return (
      <div className="mt-6 p-6 bg-gradient-to-r from-amber-50 to-orange-50 dark:from-amber-900/20 dark:to-orange-900/20 rounded-lg border border-amber-200 dark:border-amber-800">
        <div className="flex flex-col sm:flex-row items-start sm:items-center gap-4">
          <div className="flex-shrink-0">
            <div className="w-12 h-12 rounded-full bg-amber-100 dark:bg-amber-900/50 flex items-center justify-center">
              <span className="text-2xl">📋</span>
            </div>
          </div>
          <div className="flex-1">
            <h4 className="text-sm font-semibold text-amber-900 dark:text-amber-200 mb-1">
              No Infrastructure Templates Selected
            </h4>
            <p className="text-sm text-amber-700 dark:text-amber-300 mb-2">
              Select at least one infrastructure template from the project cards above to continue to deployment.
            </p>
            <div className="flex items-center gap-2 text-xs text-amber-600 dark:text-amber-400">
              <span className="font-medium">Tip:</span>
              <span>Check the boxes next to Storage, Cosmos DB, App Service, or Key Vault to select templates.</span>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className={`mt-6 p-4 rounded-lg border transition-all duration-300 ${
      readyToProceed
        ? 'bg-gradient-to-r from-green-50 to-emerald-50 dark:from-green-900/20 dark:to-emerald-900/20 border-green-200 dark:border-green-800'
        : 'bg-gray-50 dark:bg-gray-800 border-gray-200 dark:border-gray-700'
    }`}>
      <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
        <div className="flex-1 min-w-0">
          <div className="flex items-center gap-2 mb-2">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">Selected Templates:</span>
            <span className="px-2 py-0.5 text-xs bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded-full font-medium">
              {selectedTemplates.length}
            </span>
          </div>
          <div className="flex items-center gap-2 flex-wrap">
            {selectedTemplates.map(template => (
              <span
                key={template.id}
                className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded-full font-medium transition-all duration-200 hover:bg-blue-200 dark:hover:bg-blue-700"
                title={`${template.name} template selected`}
              >
                {template.name}
              </span>
            ))}
          </div>
        </div>

        <div className="flex items-center gap-4 flex-shrink-0">
          {isLoading ? (
            <div className="flex items-center gap-2 text-sm text-blue-600 dark:text-blue-400">
              <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-current"></div>
              <span>Loading status...</span>
            </div>
          ) : readyToProceed ? (
            <div className="flex items-center gap-2 text-sm text-green-700 dark:text-green-300">
              <div className="w-6 h-6 rounded-full bg-green-100 dark:bg-green-900/50 flex items-center justify-center">
                <svg className="w-4 h-4 text-green-600 dark:text-green-400" fill="currentColor" viewBox="0 0 20 20">
                  <path fillRule="evenodd" d="M16.707 5.293a1 1 0 010 1.414l-8 8a1 1 0 01-1.414 0l-4-4a1 1 0 011.414-1.414L8 12.586l7.293-7.293a1 1 0 011.414 0z" clipRule="evenodd" />
                </svg>
              </div>
              <span className="font-medium">Ready to proceed</span>
            </div>
          ) : (
            <div className="flex items-center gap-2 text-sm text-amber-700 dark:text-amber-300">
              <span className="text-amber-600 dark:text-amber-400">⚠</span>
              <span>Select more templates</span>
            </div>
          )}
          <button
            disabled={!readyToProceed}
            onClick={handleContinue}
            className={`px-6 py-2.5 text-sm font-medium rounded-lg transition-all duration-200 focus:outline-none focus:ring-2 focus:ring-offset-2 dark:focus:ring-offset-gray-900 ${
              readyToProceed
                ? 'bg-blue-600 dark:bg-blue-500 text-white hover:bg-blue-700 dark:hover:bg-blue-600 hover:shadow-lg hover:shadow-blue-500/25 focus:ring-blue-500'
                : 'bg-gray-300 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed focus:ring-gray-400'
            }`}
            title={!readyToProceed ? (isLoading ? 'Please wait for infrastructure status to finish loading...' : 'Select at least one infrastructure template to continue') : 'Continue to Step 2: Infrastructure Actions'}
          >
            Continue to Step 2 →
          </button>
        </div>
      </div>
    </div>
  );
}

