export function getProjectTypeIcon(type: string): string {
  switch (type) {
    case 'api': return '🌐';
    case 'admin-api': return '🔧';
    case 'pwa': return '📱';
    case 'service': return '⚙️';
    default: return '📦';
  }
}

export function getProjectTypeColor(type: string): string {
  switch (type) {
    case 'api': return 'bg-blue-100 dark:bg-blue-900 text-blue-800 dark:text-blue-200';
    case 'admin-api': return 'bg-purple-100 dark:bg-purple-900 text-purple-800 dark:text-purple-200';
    case 'pwa': return 'bg-green-100 dark:bg-green-900 text-green-800 dark:text-green-200';
    case 'service': return 'bg-orange-100 dark:bg-orange-900 text-orange-800 dark:text-orange-200';
    default: return 'bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200';
  }
}

export function getProjectTypeDescription(type: string): string {
  switch (type) {
    case 'api': return 'REST API service - Public-facing API endpoints for client applications';
    case 'admin-api': return 'Admin API service - Administrative API endpoints for content management';
    case 'pwa': return 'Progressive Web App - Client-facing web application with offline support';
    case 'service': return 'Background service - Long-running service for background tasks';
    default: return 'Project component';
  }
}

export function getStatusColor(status: string, conclusion: string | null): string {
  if (status === 'completed') {
    return conclusion === 'success' ? 'text-green-600 dark:text-green-400' : 'text-red-600 dark:text-red-400';
  }
  if (status === 'in_progress' || status === 'queued') {
    return 'text-blue-600 dark:text-blue-400';
  }
  return 'text-gray-600 dark:text-gray-400';
}

export function getStatusIcon(status: string, conclusion: string | null): string {
  if (status === 'completed') {
    return conclusion === 'success' ? '✅' : '❌';
  }
  if (status === 'in_progress') {
    return '🔄';
  }
  if (status === 'queued') {
    return '⏳';
  }
  return '⚪';
}
