import { MigrationStep } from './types';

interface MigrationStepIndicatorProps {
  currentStep: MigrationStep;
}

export function MigrationStepIndicator({ currentStep }: MigrationStepIndicatorProps) {
  return (
    <div className="mb-8">
      <div className="flex items-center justify-center space-x-4">
        <div className={`flex items-center ${currentStep === 'configure' ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'}`}>
          <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
            currentStep === 'configure' ? 'border-blue-600 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/30' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800'
          }`}>
            1
          </div>
          <span className="ml-2 font-medium">Configure</span>
        </div>

        <div className="w-16 h-0.5 bg-gray-300 dark:bg-gray-600"></div>

        <div className={`flex items-center ${currentStep === 'select' ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'}`}>
          <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
            currentStep === 'select' ? 'border-blue-600 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/30' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800'
          }`}>
            2
          </div>
          <span className="ml-2 font-medium">Select Resources</span>
        </div>

        <div className="w-16 h-0.5 bg-gray-300 dark:bg-gray-600"></div>

        <div className={`flex items-center ${currentStep === 'running' || currentStep === 'complete' ? 'text-blue-600 dark:text-blue-400' : 'text-gray-400 dark:text-gray-500'}`}>
          <div className={`w-8 h-8 rounded-full flex items-center justify-center border-2 ${
            currentStep === 'running' || currentStep === 'complete' ? 'border-blue-600 dark:border-blue-400 bg-blue-50 dark:bg-blue-900/30' : 'border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800'
          }`}>
            3
          </div>
          <span className="ml-2 font-medium">Migrate</span>
        </div>
      </div>
    </div>
  );
}

