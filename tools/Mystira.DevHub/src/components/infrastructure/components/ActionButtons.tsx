import type { ActionButtonsProps } from '../types';

const variantStyles = {
  primary: 'bg-blue-50 dark:bg-blue-900/30 border-blue-200 dark:border-blue-800 hover:bg-blue-100 dark:hover:bg-blue-900/50',
  warning: 'bg-yellow-50 dark:bg-yellow-900/30 border-yellow-200 dark:border-yellow-800 hover:bg-yellow-100 dark:hover:bg-yellow-900/50',
  success: 'bg-green-50 dark:bg-green-900/30 border-green-200 dark:border-green-800 hover:bg-green-100 dark:hover:bg-green-900/50',
  danger: 'bg-red-50 dark:bg-red-900/30 border-red-200 dark:border-red-800 hover:bg-red-100 dark:hover:bg-red-900/50',
};

function ActionButtons({ buttons, loading }: ActionButtonsProps) {
  return (
    <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-6">
      {buttons.map((button) => (
        <button
          key={button.id}
          onClick={button.onClick}
          disabled={button.disabled}
          className={`p-4 rounded-lg border-2 transition-all ${
            button.disabled
              ? 'opacity-50 cursor-not-allowed bg-gray-100 dark:bg-gray-800 border-gray-200 dark:border-gray-700'
              : variantStyles[button.variant]
          }`}
        >
          <div className="text-2xl mb-2">{button.icon}</div>
          <div className="font-semibold text-gray-900 dark:text-white">{button.label}</div>
          <div className="text-xs text-gray-500 dark:text-gray-400">{button.description}</div>
          {button.loading && loading && (
            <div className="mt-2">
              <span className="inline-block animate-spin rounded-full h-4 w-4 border-b-2 border-current"></span>
            </div>
          )}
        </button>
      ))}
    </div>
  );
}

export default ActionButtons;
