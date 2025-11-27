interface ServiceCardControlsProps {
  isRunning: boolean;
  isLoading: boolean;
  isBuilding: boolean;
  statusMsg?: string;
  onStart: () => void;
  onStop: () => void;
  onRebuild?: () => void;
}

export function ServiceCardControls({
  isRunning,
  isLoading,
  isBuilding,
  statusMsg,
  onStart,
  onStop,
  onRebuild,
}: ServiceCardControlsProps) {
  return (
    <div className="flex items-center gap-1">
      {onRebuild && (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onRebuild();
          }}
          disabled={isLoading || isBuilding}
          className="px-1.5 py-0.5 bg-blue-600 text-white rounded text-[10px] font-bold hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          title="Rebuild service"
        >
          🔨
        </button>
      )}
      {isRunning ? (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onStop();
          }}
          disabled={isLoading}
          className="px-2 py-0.5 bg-red-600 text-white rounded text-[10px] font-bold hover:bg-red-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors uppercase"
        >
          {isLoading ? 'STOPPING' : 'STOP'}
        </button>
      ) : (
        <button
          onClick={(e) => {
            e.stopPropagation();
            onStart();
          }}
          disabled={isLoading || isBuilding}
          className="px-2 py-0.5 bg-green-600 text-white rounded text-[10px] font-bold hover:bg-green-700 disabled:opacity-50 disabled:cursor-not-allowed transition-colors uppercase"
          title={
            isBuilding
              ? 'Service is currently building. Please wait for the build to complete.'
              : statusMsg || ''
          }
        >
          {isLoading ? (statusMsg || 'STARTING') : isBuilding ? 'BUILDING' : 'START'}
        </button>
      )}
    </div>
  );
}

