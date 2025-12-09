import { invoke } from '@tauri-apps/api/tauri';
import { useCallback, useState } from 'react';
import type { CommandResponse } from '../../../types';

interface UseInfrastructureEnvironmentParams {
  initialEnvironment: string;
  onEnvironmentChanged: (env: string) => void;
  onResetState: () => void;
}

export function useInfrastructureEnvironment({
  initialEnvironment,
  onEnvironmentChanged,
  onResetState,
}: UseInfrastructureEnvironmentParams) {
  const [environment, setEnvironment] = useState<string>(initialEnvironment);
  const [showProdConfirm, setShowProdConfirm] = useState(false);
  const [pendingEnvironment, setPendingEnvironment] = useState<string>(initialEnvironment);

  const handleEnvironmentChange = useCallback(async (newEnv: string) => {
    if (newEnv === 'prod') {
      try {
        const ownerCheck = await invoke<CommandResponse<{ isOwner: boolean; userName: string }>>('check_subscription_owner');
        if (ownerCheck.success && ownerCheck.result?.isOwner) {
          setPendingEnvironment(newEnv);
          setShowProdConfirm(true);
        } else {
          alert('Access Denied: You must have Subscription Owner role to switch to production environment.\n\n' +
                `Current user: ${ownerCheck.result?.userName || 'Unknown'}\n` +
                'Please contact your subscription administrator.');
          return;
        }
      } catch (error) {
        console.error('Failed to check subscription owner:', error);
        alert('Failed to verify subscription owner role. Cannot switch to production environment.');
        return;
      }
    } else {
      setEnvironment(newEnv);
      onEnvironmentChanged(newEnv);
      onResetState();
    }
  }, [onEnvironmentChanged, onResetState]);

  const confirmProdSwitch = useCallback(() => {
    const newEnv = pendingEnvironment;
    setEnvironment(newEnv);
    onEnvironmentChanged(newEnv);
    onResetState();
    setShowProdConfirm(false);
  }, [pendingEnvironment, onEnvironmentChanged, onResetState]);

  const cancelProdSwitch = useCallback(() => {
    setShowProdConfirm(false);
    setPendingEnvironment(environment);
  }, [environment]);

  return {
    environment,
    showProdConfirm,
    pendingEnvironment,
    handleEnvironmentChange,
    confirmProdSwitch,
    cancelProdSwitch,
  };
}

