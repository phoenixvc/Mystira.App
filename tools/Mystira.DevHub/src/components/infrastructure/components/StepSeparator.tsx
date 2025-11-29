interface StepSeparatorProps {
  label: string;
}

export function StepSeparator({ label }: StepSeparatorProps) {
  return (
    <div className="mb-6 relative">
      <div className="absolute inset-0 flex items-center">
        <div className="w-full border-t border-gray-300 dark:border-gray-600"></div>
      </div>
      <div className="relative flex justify-center">
        <span className="px-4 py-1 bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 rounded-full border border-gray-300 dark:border-gray-600">
          {label}
        </span>
      </div>
    </div>
  );
}

