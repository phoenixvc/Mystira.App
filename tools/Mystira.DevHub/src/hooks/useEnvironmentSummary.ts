export function useEnvironmentSummary(serviceEnvironments: Record<string, 'local' | 'dev' | 'prod'>): string {
  const localCount = Object.values(serviceEnvironments).filter(e => e === 'local' || !e).length;
  const devCount = Object.values(serviceEnvironments).filter(e => e === 'dev').length;
  const prodCount = Object.values(serviceEnvironments).filter(e => e === 'prod').length;

  const parts: string[] = [];
  if (localCount > 0) parts.push(`${localCount} Local`);
  if (devCount > 0) parts.push(`${devCount} Dev`);
  if (prodCount > 0) parts.push(`${prodCount} Prod`);

  return parts.length > 0 ? parts.join(' | ') : 'All Local';
}

