import { invoke } from '@tauri-apps/api/tauri';
import { useEffect, useState } from 'react';
import type { CommandResponse, WorkflowStatus } from '../../../types';

export function useWorkflowStatus(workflowFile: string, repository: string) {
  const [status, setStatus] = useState<WorkflowStatus | null>(null);

  const fetchStatus = async () => {
    try {
      const response: CommandResponse<WorkflowStatus> = await invoke('infrastructure_status', {
        workflowFile,
        repository,
      });
      if (response.success && response.result) {
        setStatus(response.result);
      }
    } catch (error) {
      console.error('Failed to fetch workflow status:', error);
    }
  };

  useEffect(() => {
    fetchStatus();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  return { status, fetchStatus };
}

