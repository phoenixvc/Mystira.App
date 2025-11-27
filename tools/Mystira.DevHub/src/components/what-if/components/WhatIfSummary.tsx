interface WhatIfSummaryProps {
  createCount: number;
  modifyCount: number;
  deleteCount: number;
  noChangeCount: number;
  compact: boolean;
}

export function WhatIfSummary({
  createCount,
  modifyCount,
  deleteCount,
  noChangeCount,
  compact,
}: WhatIfSummaryProps) {
  return (
    <div className={`flex gap-2 flex-wrap ${compact ? 'mb-2' : 'mb-4'}`}>
      {createCount > 0 && (
        <div
          className={`bg-white dark:bg-gray-800 border border-green-200 dark:border-green-800 rounded ${
            compact ? 'px-2 py-1' : 'p-3'
          } text-center flex items-center gap-1`}
        >
          <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-green-700 dark:text-green-400`}>
            {createCount}
          </span>
          <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-green-600 dark:text-green-500 font-medium`}>
            Create
          </span>
        </div>
      )}
      {modifyCount > 0 && (
        <div
          className={`bg-white dark:bg-gray-800 border border-yellow-200 dark:border-yellow-800 rounded ${
            compact ? 'px-2 py-1' : 'p-3'
          } text-center flex items-center gap-1`}
        >
          <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-yellow-700 dark:text-yellow-400`}>
            {modifyCount}
          </span>
          <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-yellow-600 dark:text-yellow-500 font-medium`}>
            Modify
          </span>
        </div>
      )}
      {deleteCount > 0 && (
        <div
          className={`bg-white dark:bg-gray-800 border border-red-200 dark:border-red-800 rounded ${
            compact ? 'px-2 py-1' : 'p-3'
          } text-center flex items-center gap-1`}
        >
          <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-red-700 dark:text-red-400`}>
            {deleteCount}
          </span>
          <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-red-600 dark:text-red-500 font-medium`}>
            Delete
          </span>
        </div>
      )}
      {noChangeCount > 0 && (
        <div
          className={`bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded ${
            compact ? 'px-2 py-1' : 'p-3'
          } text-center flex items-center gap-1`}
        >
          <span className={`${compact ? 'text-sm' : 'text-2xl'} font-bold text-gray-700 dark:text-gray-400`}>
            {noChangeCount}
          </span>
          <span className={`${compact ? 'text-[10px]' : 'text-xs'} text-gray-600 dark:text-gray-500 font-medium`}>
            No Change
          </span>
        </div>
      )}
    </div>
  );
}

