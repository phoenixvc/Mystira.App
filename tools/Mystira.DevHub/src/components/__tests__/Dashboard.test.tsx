import { screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useConnectionStore } from '../../stores/connectionStore';
import { mockConnectionTestSuccess, mockTauriInvoke, renderWithProviders } from '../../test/utils';
import { Dashboard } from '../dashboard';

describe('Dashboard', () => {
  const mockNavigate = vi.fn();

  beforeEach(() => {
    useConnectionStore.getState().reset();
    vi.clearAllMocks();
  });

  it('should render page title', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    expect(screen.getByText('Welcome to Mystira DevHub')).toBeInTheDocument();
    expect(screen.getByText('Your central hub for development operations and data management')).toBeInTheDocument();
  });

  it('should render connection status section', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    expect(screen.getByRole('heading', { name: 'Connection Status' })).toBeInTheDocument();
    expect(screen.getByText('Cosmos DB')).toBeInTheDocument();
    expect(screen.getByText('Azure CLI')).toBeInTheDocument();
    expect(screen.getByText('GitHub CLI')).toBeInTheDocument();
    expect(screen.getByText('Blob Storage')).toBeInTheDocument();
  });

  it('should render quick actions', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    expect(screen.getByRole('heading', { name: 'Quick Actions' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Export Sessions/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /View Statistics/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Run Migration/i })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: /Validate Infrastructure/i })).toBeInTheDocument();
  });

  it('should handle quick action clicks', async () => {
    const user = userEvent.setup();
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    const exportButton = screen.getByRole('button', { name: /Export Sessions/i });
    await user.click(exportButton);

    expect(mockNavigate).toHaveBeenCalledWith('cosmos');
  });

  it('should test connections on mount', async () => {
    const { invoke } = vi.mocked(await import('@tauri-apps/api/tauri'));
    invoke.mockResolvedValue(mockConnectionTestSuccess('test', {}));

    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    await waitFor(() => {
      expect(invoke).toHaveBeenCalled();
    });
  });

  it('should display recent operations', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    expect(screen.getByRole('heading', { name: 'Recent Operations' })).toBeInTheDocument();
    expect(screen.getByText('Export Game Sessions')).toBeInTheDocument();
    expect(screen.getByText('Scenario Statistics')).toBeInTheDocument();
  });

  it('should have proper semantic HTML structure', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    expect(screen.getByRole('main')).toBeInTheDocument();
    expect(screen.getByRole('banner')).toBeInTheDocument(); // header
    expect(screen.getAllByRole('heading', { level: 2 })).toHaveLength(3); // h2 elements
  });

  it('should have accessible connection status cards', () => {
    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    const cosmosCard = screen.getByLabelText(/Cosmos DB: checking/i);
    expect(cosmosCard).toBeInTheDocument();

    const statusIndicator = screen.getAllByRole('status')[0];
    expect(statusIndicator).toBeInTheDocument();
  });

  it('should handle connection errors gracefully', async () => {
    await mockTauriInvoke('test_connection', {
      success: false,
      error: 'Connection timeout',
    });

    useConnectionStore.getState().setConnectionStatus('azurecli', {
      status: 'disconnected',
      error: 'Connection timeout',
    });

    renderWithProviders(<Dashboard onNavigate={mockNavigate} />);

    await waitFor(() => {
      expect(screen.getByText('Connection timeout')).toBeInTheDocument();
    });
  });
});
