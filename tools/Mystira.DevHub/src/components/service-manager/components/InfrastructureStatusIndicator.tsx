interface InfrastructureStatusIndicatorProps {
  serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>;
  infrastructureStatus: {
    dev: { exists: boolean; checking: boolean };
    prod: { exists: boolean; checking: boolean };
  };
}

export function InfrastructureStatusIndicator({
  serviceEnvironments,
  infrastructureStatus,
}: InfrastructureStatusIndicatorProps) {
  const hasDevServices = Object.values(serviceEnvironments).includes('dev');
  const hasProdServices = Object.values(serviceEnvironments).includes('prod');
  const devStatus = infrastructureStatus.dev;
  const prodStatus = infrastructureStatus.prod;

  if (!hasDevServices && !hasProdServices) {
    return null;
  }

  const handleDeploy = () => {
    window.dispatchEvent(new CustomEvent('navigate-to-infrastructure'));
  };

  return (
    <div className="flex items-center gap-2 text-xs">
      {hasDevServices && (
        <div
          className="flex items-center gap-1 px-2 py-1 rounded"
          style={{
            backgroundColor: devStatus.checking
              ? '#fef3c7'
              : devStatus.exists
              ? '#d1fae5'
              : '#fee2e2',
            color: devStatus.checking
              ? '#92400e'
              : devStatus.exists
              ? '#065f46'
              : '#991b1b',
          }}
        >
          {devStatus.checking ? '⏳' : devStatus.exists ? '✅' : '⚠️'}
          <span className="font-medium">DEV</span>
          {!devStatus.exists && !devStatus.checking && (
            <button
              onClick={handleDeploy}
              className="ml-1 underline hover:no-underline"
              title="Deploy missing infrastructure"
            >
              Deploy
            </button>
          )}
        </div>
      )}
      {hasProdServices && (
        <div
          className="flex items-center gap-1 px-2 py-1 rounded"
          style={{
            backgroundColor: prodStatus.checking
              ? '#fef3c7'
              : prodStatus.exists
              ? '#d1fae5'
              : '#fee2e2',
            color: prodStatus.checking
              ? '#92400e'
              : prodStatus.exists
              ? '#065f46'
              : '#991b1b',
          }}
        >
          {prodStatus.checking ? '⏳' : prodStatus.exists ? '✅' : '⚠️'}
          <span className="font-medium">PROD</span>
          {!prodStatus.exists && !prodStatus.checking && (
            <button
              onClick={handleDeploy}
              className="ml-1 underline hover:no-underline"
              title="Deploy missing infrastructure"
            >
              Deploy
            </button>
          )}
        </div>
      )}
    </div>
  );
}

