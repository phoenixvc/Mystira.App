
export interface EnvironmentPreset {
  id: string;
  name: string;
  description: string;
  environments: Record<string, 'local' | 'dev' | 'prod'>;
}

export const DEFAULT_PRESETS: EnvironmentPreset[] = [
  {
    id: 'all-local',
    name: 'Full Local',
    description: 'All services on localhost',
    environments: {
      'api': 'local',
      'admin-api': 'local',
      'pwa': 'local',
    },
  },
  {
    id: 'api-dev-rest-local',
    name: 'API Dev, Rest Local',
    description: 'API on dev, others on local',
    environments: {
      'api': 'dev',
      'admin-api': 'local',
      'pwa': 'local',
    },
  },
  {
    id: 'full-dev',
    name: 'Full Dev',
    description: 'All services on dev environment',
    environments: {
      'api': 'dev',
      'admin-api': 'dev',
      'pwa': 'dev',
    },
  },
  {
    id: 'testing-prod',
    name: 'Testing Prod',
    description: 'All services on production (with warnings)',
    environments: {
      'api': 'prod',
      'admin-api': 'prod',
      'pwa': 'prod',
    },
  },
];

export function savePreset(preset: EnvironmentPreset): void {
  const saved = getSavedPresets();
  const updated = [...saved.filter(p => p.id !== preset.id), preset];
  localStorage.setItem('environmentPresets', JSON.stringify(updated));
}

export function getSavedPresets(): EnvironmentPreset[] {
  const saved = localStorage.getItem('environmentPresets');
  return saved ? JSON.parse(saved) : [];
}

export function deletePreset(presetId: string): void {
  const saved = getSavedPresets();
  const updated = saved.filter(p => p.id !== presetId);
  localStorage.setItem('environmentPresets', JSON.stringify(updated));
}

export function getAllPresets(): EnvironmentPreset[] {
  return [...DEFAULT_PRESETS, ...getSavedPresets()];
}

