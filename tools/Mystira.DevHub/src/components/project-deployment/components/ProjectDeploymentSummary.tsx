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

  return (
    <div className="mt-6 flex items-center justify-between gap-4 p-4 bg-gray-50 dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700">
      <div className="flex-1 min-w-0 flex items-center gap-2 flex-wrap">
        <span className="text-xs font-medium text-gray-700 dark:text-gray-300">Selected Templates:</span>
        {selectedTemplates.length > 0 ? (
          selectedTemplates.map(template => (
            <span
              key={template.id}
              className="px-2 py-1 text-xs bg-blue-100 dark:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
            >
              {template.name}
            </span>
          ))
        ) : (
          <span className="text-xs text-gray-500 dark:text-gray-400 italic">
            No templates selected
          </span>
        )}
      </div>

      <div className="flex items-center gap-3 flex-shrink-0">
        {readyToProceed ? (
          <div className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <span className="text-green-600 dark:text-green-400">✓</span>
            <span>Ready</span>
          </div>
        ) : (
          <div className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
            <span className="text-yellow-600 dark:text-yellow-400">⚠</span>
            <span>Select templates</span>
          </div>
        )}
        <button
          disabled={!readyToProceed}
          onClick={handleContinue}
          className={`px-6 py-2 text-sm font-medium rounded-lg transition-colors ${
            readyToProceed
              ? 'bg-blue-600 dark:bg-blue-500 text-white hover:bg-blue-700 dark:hover:bg-blue-600'
              : 'bg-gray-300 dark:bg-gray-700 text-gray-500 dark:text-gray-400 cursor-not-allowed'
          }`}
          title={!readyToProceed ? (infrastructureLoading || loadingStatus ? 'Please wait for infrastructure status to finish loading...' : 'Select at least one infrastructure template to continue') : 'Continue to Step 2: Infrastructure Actions'}
        >
          Continue to Step 2 →
        </button>
      </div>
    </div>
  );
}

