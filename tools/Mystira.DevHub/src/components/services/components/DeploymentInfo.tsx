
export interface DeploymentInfo {
  serviceName: string;
  environment: 'dev' | 'prod';
  lastDeployed?: number; // timestamp
  deploymentStatus?: 'success' | 'failed' | 'in-progress';
  commitSha?: string;
  branch?: string;
  deployedBy?: string;
  workflowRunUrl?: string;
}

// Mock function to fetch deployment info from GitHub Actions
// In a real implementation, this would call a Tauri command that queries GitHub API
export async function fetchDeploymentInfo(
  _serviceName: string,
  _environment: 'dev' | 'prod'
): Promise<DeploymentInfo | null> {
  // TODO: Implement actual GitHub Actions API integration
  // For now, return null to indicate no deployment info available
  return null;
}

// Format deployment info for display
export function formatDeploymentInfo(info: DeploymentInfo | null): {
  text: string;
  tooltip: string;
  statusColor: string;
} {
  if (!info || !info.lastDeployed) {
    return {
      text: 'No deployment info',
      tooltip: 'Deployment information not available',
      statusColor: 'text-gray-400',
    };
  }
  
  const deployedDate = new Date(info.lastDeployed);
  const timeSince = Math.floor((Date.now() - info.lastDeployed) / 1000);
  let timeText: string;
  
  if (timeSince < 60) {
    timeText = `${timeSince}s ago`;
  } else if (timeSince < 3600) {
    timeText = `${Math.floor(timeSince / 60)}m ago`;
  } else if (timeSince < 86400) {
    timeText = `${Math.floor(timeSince / 3600)}h ago`;
  } else {
    timeText = `${Math.floor(timeSince / 86400)}d ago`;
  }
  
  const statusIcon = info.deploymentStatus === 'success' ? '✓' :
                     info.deploymentStatus === 'failed' ? '✗' :
                     info.deploymentStatus === 'in-progress' ? '⟳' : '?';
  
  const statusColor = info.deploymentStatus === 'success' ? 'text-green-500' :
                      info.deploymentStatus === 'failed' ? 'text-red-500' :
                      info.deploymentStatus === 'in-progress' ? 'text-yellow-500' : 'text-gray-400';
  
  const tooltip = [
    `Deployed: ${deployedDate.toLocaleString()}`,
    info.branch ? `Branch: ${info.branch}` : '',
    info.commitSha ? `Commit: ${info.commitSha.substring(0, 7)}` : '',
    info.deployedBy ? `By: ${info.deployedBy}` : '',
  ].filter(Boolean).join('\n');
  
  return {
    text: `${statusIcon} Deployed ${timeText}`,
    tooltip,
    statusColor,
  };
}

