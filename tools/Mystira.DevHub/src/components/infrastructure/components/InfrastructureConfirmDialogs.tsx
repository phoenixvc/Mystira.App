import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse, TemplateConfig, WhatIfChange } from '../../../types';
import { ConfirmDialog } from '../../ConfirmDialog';

interface InfrastructureConfirmDialogsProps {
  showDestroyConfirm: boolean;
  showDestroySelect: boolean;
  showProdConfirm: boolean;
  showDeployConfirm: boolean;
  showResourceGroupConfirm: boolean;
  pendingResourceGroup: { resourceGroup: string; location: string } | null;
  whatIfChanges: WhatIfChange[];
  templates: TemplateConfig[];
  environment: string;
  onDestroyConfirm: () => void;
  onDestroyCancel: () => void;
  onProdConfirm: () => void;
  onProdCancel: () => void;
  onDeployConfirm: () => Promise<void>;
  onDeployCancel: () => void;
  onResourceGroupConfirm: () => Promise<void>;
  onResourceGroupCancel: () => void;
  onSetLoading: (loading: boolean) => void;
  onSetCurrentAction: (action: 'deploy' | null) => void;
  onSetDeploymentProgress: (progress: string | null) => void;
  onSetLastResponse: (response: CommandResponse | null) => void;
  onSetPendingResourceGroup: (rg: { resourceGroup: string; location: string } | null) => void;
}

export function InfrastructureConfirmDialogs({
  showDestroyConfirm,
  showDestroySelect,
  showProdConfirm,
  showDeployConfirm,
  showResourceGroupConfirm,
  pendingResourceGroup,
  whatIfChanges,
  templates,
  environment,
  onDestroyConfirm,
  onDestroyCancel,
  onProdConfirm,
  onProdCancel,
  onDeployConfirm,
  onDeployCancel,
  onResourceGroupConfirm,
  onResourceGroupCancel,
  onSetLoading,
  onSetCurrentAction,
  onSetDeploymentProgress,
  onSetLastResponse,
  onSetPendingResourceGroup,
}: InfrastructureConfirmDialogsProps) {
  const handleResourceGroupConfirm = async () => {
    if (!pendingResourceGroup) return;
    
    onResourceGroupCancel();
    onSetLoading(true);
    onSetCurrentAction('deploy');
    onSetDeploymentProgress(`Creating resource group '${pendingResourceGroup.resourceGroup}'...`);
    
    try {
      const createRgResponse = await invoke<CommandResponse>('azure_create_resource_group', {
        resourceGroup: pendingResourceGroup.resourceGroup,
        location: pendingResourceGroup.location,
      });
      
      if (!createRgResponse.success) {
        onSetLastResponse({
          success: false,
          error: createRgResponse.error || `Failed to create resource group '${pendingResourceGroup.resourceGroup}'`,
        });
        onSetLoading(false);
        onSetCurrentAction(null);
        onSetPendingResourceGroup(null);
        return;
      }
      
      onSetDeploymentProgress(`Deploying to ${pendingResourceGroup.resourceGroup}...`);
      await onResourceGroupConfirm();
    } catch (error) {
      onSetLastResponse({
        success: false,
        error: `Failed to create resource group: ${String(error)}`,
      });
      onSetLoading(false);
      onSetCurrentAction(null);
    } finally {
      onSetPendingResourceGroup(null);
    }
  };

  const selectedDestroyResources = whatIfChanges.filter(
    c => c.selected !== false && (c.changeType === 'delete' || c.selected === true)
  );
  
  const deployCount = whatIfChanges.length > 0
    ? whatIfChanges.filter(c => c.selected !== false).length
    : templates.filter(t => t.selected).length;

  return (
    <>
      <ConfirmDialog
        isOpen={showDestroyConfirm && !showDestroySelect}
        title="⚠️ Destroy All Infrastructure"
        message="This will permanently delete ALL infrastructure resources. This action cannot be undone!"
        confirmText="Yes, Destroy Everything"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700"
        requireTextMatch="DELETE"
        onConfirm={onDestroyConfirm}
        onCancel={onDestroyCancel}
      />
      
      <ConfirmDialog
        isOpen={showProdConfirm}
        title="⚠️ Production Environment Warning"
        message="You are about to switch to the PRODUCTION environment. All operations (validate, preview, deploy, destroy) will affect production resources. This is a critical environment with real users and data. Are you absolutely sure you want to proceed?"
        confirmText="Yes, Switch to Production"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="PRODUCTION"
        onConfirm={onProdConfirm}
        onCancel={onProdCancel}
      />
      
      <ConfirmDialog
        isOpen={showDestroySelect}
        title="💥 Destroy Selected Resources"
        message={`⚠️ WARNING: You are about to permanently DELETE ${selectedDestroyResources.length} selected resource(s) from Azure.\n\nThis action CANNOT be undone and will permanently remove:\n${selectedDestroyResources.map(c => `  • ${c.resourceName} (${c.resourceType})`).join('\n')}\n\nType "DELETE" in the field below to confirm.`}
        confirmText="Yes, Destroy Selected"
        cancelText="Cancel"
        confirmButtonClass="bg-red-600 hover:bg-red-700 dark:bg-red-500 dark:hover:bg-red-600"
        requireTextMatch="DELETE"
        onConfirm={onDestroyConfirm}
        onCancel={onDestroyCancel}
      />
      
      <ConfirmDialog
        isOpen={showDeployConfirm}
        title="🚀 Deploy Selected Resources"
        message={`You are about to deploy ${deployCount} ${whatIfChanges.length > 0 ? 'selected resource(s)' : 'template(s)'} to ${environment} environment.`}
        confirmText="Deploy Selected Resources"
        cancelText="Cancel"
        confirmButtonClass="bg-green-600 hover:bg-green-700"
        onConfirm={onDeployConfirm}
        onCancel={onDeployCancel}
      />
      
      <ConfirmDialog
        isOpen={showResourceGroupConfirm}
        title="📦 Create Resource Group"
        message={pendingResourceGroup 
          ? `The resource group "${pendingResourceGroup.resourceGroup}" does not exist in location "${pendingResourceGroup.location}".\n\nWould you like to create it now? This is required before deploying infrastructure.`
          : ''}
        confirmText="Create Resource Group"
        cancelText="Cancel"
        confirmButtonClass="bg-blue-600 hover:bg-blue-700"
        onConfirm={handleResourceGroupConfirm}
        onCancel={onResourceGroupCancel}
      />
    </>
  );
}

