import { useState } from 'react';
import type { CommandResponse, CosmosWarning, ResourceGroupConvention, TemplateConfig, WhatIfChange, WorkflowStatus } from '../../../types';
import { DEFAULT_PROJECTS } from '../../../types';
import InfrastructureStatus from '../../InfrastructureStatus';
import ProjectDeployment from '../../ProjectDeployment';
import ProjectDeploymentPlanner from '../../ProjectDeploymentPlanner';
import ResourceGroupConfig from '../../ResourceGroupConfig';
import TemplateEditor from '../../TemplateEditor';
import { DeploymentProgress } from './DeploymentProgress';
import { InfrastructureActionButtons } from './InfrastructureActionButtons';
import { InfrastructureProgressStepper } from './InfrastructureProgressStepper';
import { InfrastructureResponseDisplay } from './InfrastructureResponseDisplay';
import { ReadyToDeployBanner } from './ReadyToDeployBanner';
import { StepSeparator } from './StepSeparator';
import { WhatIfViewerSection } from './WhatIfViewerSection';
import { WorkflowStatusDisplay } from './WorkflowStatusDisplay';

interface InfrastructureActionsTabProps {
  environment: string;
  templates: TemplateConfig[];
  onTemplatesChange: (templates: TemplateConfig[]) => void;
  resourceGroupConfig: ResourceGroupConvention;
  onResourceGroupConfigChange: (config: ResourceGroupConvention) => void;
  step1Collapsed: boolean;
  onStep1CollapsedChange: (collapsed: boolean) => void;
  showStep2: boolean;
  onShowStep2Change: (show: boolean) => void;
  hasValidated: boolean;
  hasPreviewed: boolean;
  loading: boolean;
  currentAction: 'validate' | 'preview' | 'deploy' | 'destroy' | null;
  onAction: (action: 'validate' | 'preview' | 'deploy' | 'destroy') => Promise<void>;
  lastResponse: CommandResponse | null;
  whatIfChanges: WhatIfChange[];
  onWhatIfChangesChange: (changes: WhatIfChange[]) => void;
  cosmosWarning: CosmosWarning | null;
  onCosmosWarningChange: (warning: CosmosWarning | null) => void;
  infrastructureLoading: boolean;
  onInfrastructureLoadingChange: (loading: boolean) => void;
  workflowStatus: WorkflowStatus | null;
  deploymentMethod: 'github' | 'azure-cli';
  onShowDestroySelect: () => void;
  hasDeployedInfrastructure: boolean;
  deploymentProgress: string | null;
}

export default function InfrastructureActionsTab({
  environment,
  templates,
  onTemplatesChange,
  resourceGroupConfig,
  onResourceGroupConfigChange,
  step1Collapsed,
  onStep1CollapsedChange,
  showStep2,
  onShowStep2Change,
  hasValidated,
  hasPreviewed,
  loading,
  currentAction,
  onAction,
  lastResponse,
  whatIfChanges,
  onWhatIfChangesChange,
  cosmosWarning,
  onCosmosWarningChange,
  infrastructureLoading,
  onInfrastructureLoadingChange,
  workflowStatus,
  deploymentMethod,
  onShowDestroySelect,
  hasDeployedInfrastructure,
  deploymentProgress,
}: InfrastructureActionsTabProps) {
  const [showResourceGroupConfig, setShowResourceGroupConfig] = useState(false);
  const [editingTemplate, setEditingTemplate] = useState<TemplateConfig | null>(null);
  const hasSelectedTemplates = templates.some(t => t.selected);

  return (
    <div>
      <InfrastructureProgressStepper
        templates={templates}
        hasValidated={hasValidated}
        hasPreviewed={hasPreviewed}
        showStep2={showStep2}
        step1Collapsed={step1Collapsed}
        onStep1CollapsedChange={onStep1CollapsedChange}
      />

      {/* Infrastructure Status Dashboard */}
      <div className="mb-6">
        <InfrastructureStatus
          environment={environment}
          resourceGroup={resourceGroupConfig.defaultResourceGroup || `dev-san-rg-mystira-app`}
          onStatusChange={() => {
            onInfrastructureLoadingChange(false);
          }}
          onLoadingChange={onInfrastructureLoadingChange}
        />
      </div>

      {/* Project Deployment Planner - Step 1 */}
      {!step1Collapsed && (
        <div className="mb-6">
          <ProjectDeploymentPlanner
            environment={environment}
            resourceGroupConfig={resourceGroupConfig}
            templates={templates}
            onTemplatesChange={onTemplatesChange}
            onEditTemplate={setEditingTemplate}
            region={resourceGroupConfig.region || 'san'}
            projectName={resourceGroupConfig.projectName || 'mystira-app'}
            onProceedToStep2={() => onShowStep2Change(true)}
            infrastructureLoading={infrastructureLoading}
          />
        </div>
      )}

      {/* Visual Separator */}
      {showStep2 && hasSelectedTemplates && (
        <StepSeparator label="Ready for Step 2" />
      )}

      {/* Action Buttons - Step 2 */}
      {showStep2 && (
        <InfrastructureActionButtons
          loading={loading}
          hasValidated={hasValidated}
          hasPreviewed={hasPreviewed}
          currentAction={currentAction}
          whatIfChangesLength={whatIfChanges.length}
          deploymentMethod={deploymentMethod}
          step1Collapsed={step1Collapsed}
          onAction={onAction}
          onShowDestroySelect={onShowDestroySelect}
          onStep1CollapsedChange={onStep1CollapsedChange}
          hasSelectedTemplates={hasSelectedTemplates}
        />
      )}

      {/* Response Display */}
      {lastResponse && (
        <InfrastructureResponseDisplay
          response={lastResponse}
          cosmosWarning={cosmosWarning}
          onCosmosWarningChange={onCosmosWarningChange}
          whatIfChanges={whatIfChanges}
          onLastResponseChange={(response) => {
            if (response && cosmosWarning) {
              onCosmosWarningChange({ ...cosmosWarning, dismissed: true });
              onWhatIfChangesChange(whatIfChanges);
            }
          }}
        />
      )}

      {/* What-If Viewer */}
      <WhatIfViewerSection
        whatIfChanges={whatIfChanges}
        loading={loading}
        hasPreviewed={hasPreviewed}
        deploymentMethod={deploymentMethod}
        resourceGroupConfig={resourceGroupConfig}
        onWhatIfChangesChange={onWhatIfChangesChange}
      />

      {/* Deployment Info - Show when previewed but no changes (e.g., Cosmos errors only) */}
      {hasPreviewed && whatIfChanges.length === 0 && hasSelectedTemplates && (
        <ReadyToDeployBanner templates={templates} />
      )}

      {/* Deployment Progress */}
      <DeploymentProgress progress={deploymentProgress} />

      {/* Step 3: Project Deployment - Show after successful infrastructure deployment */}
      {hasDeployedInfrastructure && (
        <div className="mb-8">
          <StepSeparator label="Step 3: Deploy Projects" />
          <ProjectDeployment
            environment={environment}
            projects={DEFAULT_PROJECTS}
            hasDeployedInfrastructure={hasDeployedInfrastructure}
          />
        </div>
      )}

      {/* Resource Group Config Modal */}
      {showResourceGroupConfig && (
        <ResourceGroupConfig
          environment={environment}
          onSave={(config) => {
            onResourceGroupConfigChange(config);
            // Update existing whatIfChanges with new resource groups
            const updated = whatIfChanges.map(change => ({
              ...change,
              resourceGroup: change.resourceGroup || 
                config.resourceTypeMappings?.[change.resourceType] || 
                config.defaultResourceGroup,
            }));
            onWhatIfChangesChange(updated);
            setShowResourceGroupConfig(false);
          }}
          onClose={() => setShowResourceGroupConfig(false)}
        />
      )}

      {/* Template Editor Modal */}
      {editingTemplate && (
        <TemplateEditor
          template={editingTemplate}
          onSave={(template, saveAsNew) => {
            if (saveAsNew) {
              // Add as new template
              const newTemplate = { ...template, id: `${template.id}-${Date.now()}` };
              onTemplatesChange([...templates, newTemplate]);
            } else {
              // Update existing template
              const updated = templates.map(t => t.id === template.id ? template : t);
              onTemplatesChange(updated);
            }
            setEditingTemplate(null);
          }}
          onClose={() => setEditingTemplate(null)}
        />
      )}

      {/* Workflow Status */}
      <WorkflowStatusDisplay workflowStatus={workflowStatus} />
    </div>
  );
}

