import { ReactElement } from 'react';
import { render, RenderOptions } from '@testing-library/react';
import { vi } from 'vitest';

/**
 * Custom render function that wraps components with providers
 */
export function renderWithProviders(
  ui: ReactElement,
  options?: Omit<RenderOptions, 'wrapper'>
) {
  return render(ui, { ...options });
}

/**
 * Mock Tauri invoke function
 */
export async function mockTauriInvoke(command: string, response: any) {
  const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
  invoke.mockImplementation((cmd: string) => {
    if (cmd === command) {
      return Promise.resolve(response);
    }
    return Promise.reject(new Error(`Unexpected command: ${cmd}`));
  });
}

/**
 * Create a mock store for testing
 */
export function createMockStore<T>(initialState: Partial<T>) {
  return {
    getState: () => initialState as T,
    setState: vi.fn(),
    subscribe: vi.fn(),
    destroy: vi.fn(),
  };
}

/**
 * Wait for async operations to complete
 */
export async function waitForAsync() {
  await new Promise((resolve) => setTimeout(resolve, 0));
}

/**
 * Mock connection test response
 */
export function mockConnectionTestSuccess(_type: string, result: any) {
  return {
    success: true,
    result: {
      connected: true,
      ...result,
    },
  };
}

/**
 * Mock connection test error
 */
export function mockConnectionTestError(error: string) {
  return {
    success: false,
    error,
  };
}

/**
 * Mock Azure resources response
 */
export function mockAzureResourcesResponse(resources: any[] = []) {
  return {
    success: true,
    result: resources,
  };
}

/**
 * Mock GitHub deployments response
 */
export function mockGitHubDeploymentsResponse(deployments: any[] = []) {
  return {
    success: true,
    result: deployments,
  };
}

/**
 * Create a test resource
 */
export function createTestResource(overrides: Partial<any> = {}) {
  return {
    id: '/subscriptions/test/resourceGroups/test-rg/providers/Microsoft.Storage/storageAccounts/testaccount',
    name: 'testaccount',
    type: 'Microsoft.Storage/storageAccounts',
    location: 'eastus',
    resourceGroup: 'test-rg',
    sku: { name: 'Standard_LRS' },
    kind: 'StorageV2',
    ...overrides,
  };
}

/**
 * Create a test deployment
 */
export function createTestDeployment(overrides: Partial<any> = {}) {
  return {
    id: 123456,
    name: 'Infrastructure Deploy',
    display_title: 'Deploy infrastructure to dev',
    status: 'completed',
    conclusion: 'success',
    created_at: new Date().toISOString(),
    updated_at: new Date().toISOString(),
    html_url: 'https://github.com/test/repo/actions/runs/123456',
    actor: {
      login: 'testuser',
    },
    ...overrides,
  };
}

// Re-export testing library utilities
export * from '@testing-library/react';
export { vi } from 'vitest';
