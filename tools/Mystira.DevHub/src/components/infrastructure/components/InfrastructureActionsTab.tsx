import { useState } from 'react';
import type { CommandResponse, CosmosWarning, ResourceGroupConvention, TemplateConfig, WhatIfChange, WorkflowStatus } from '../../../types';
import { DEFAULT_PROJECTS } from '../../../types';
import InfrastructureStatus from '../../InfrastructureStatus';
import ProjectDeployment from '../../ProjectDeployment';
import ProjectDeploymentPlanner from '../../ProjectDeploymentPlanner';
import ResourceGroupConfig from '../../ResourceGroupConfig';
import TemplateEditor from '../../TemplateEditor';
import WhatIfViewer from '../../WhatIfViewer';
import { InfrastructureResponseDisplay } from './InfrastructureResponseDisplay';

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

  return (
    <div>
      {/* Progress Stepper */}
      <div className="mb-6 px-4 py-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg border border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-4 flex-1">
            {/* Step 1 */}
            <div className="flex items-center gap-2">
              <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
                templates.some(t => t.selected) 
                  ? 'bg-blue-600 dark:bg-blue-500 text-white' 
                  : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
              }`}>
                {templates.some(t => t.selected) ? '✓' : '1'}
              </div>
              <span className={`text-sm font-medium ${
                templates.some(t => t.selected)
                  ? 'text-blue-600 dark:text-blue-400'
                  : 'text-gray-600 dark:text-gray-400'
              }`}>
                Plan Deployment
              </span>
            </div>
            
            {/* Connector - only show when Step 2 is visible */}
            {showStep2 && (
              <>
                <div className={`flex-1 h-0.5 ${
                  templates.some(t => t.selected)
                    ? 'bg-blue-600 dark:bg-blue-500'
                    : 'bg-gray-300 dark:bg-gray-600'
                }`} />
                
                {/* Step 2 */}
                <div className="flex items-center gap-2">
                  <div className={`flex items-center justify-center w-8 h-8 rounded-full font-semibold text-sm ${
                    hasValidated 
                      ? 'bg-purple-600 dark:bg-purple-500 text-white' 
                      : templates.some(t => t.selected)
                      ? 'bg-blue-600 dark:bg-blue-500 text-white'
                      : 'bg-gray-300 dark:bg-gray-600 text-gray-600 dark:text-gray-300'
                  }`}>
                    {hasPreviewed ? '✓' : '2'}
                  </div>
                  <span className={`text-sm font-medium ${
                    hasValidated || templates.some(t => t.selected)
                      ? 'text-blue-600 dark:text-blue-400'
                      : 'text-gray-600 dark:text-gray-400'
                  }`}>
                    Infrastructure Actions
                  </span>
                </div>
              </>
            )}
          </div>
          
          {/* Collapse Toggle */}
          {showStep2 && templates.some(t => t.selected) && (
            <button
              onClick={() => onStep1CollapsedChange(!step1Collapsed)}
              className="px-3 py-1.5 text-xs font-medium text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 bg-white dark:bg-gray-700 hover:bg-gray-100 dark:hover:bg-gray-600 rounded-md border border-gray-300 dark:border-gray-600 transition-colors flex items-center gap-1.5"
              title={step1Collapsed ? 'Expand Step 1' : 'Collapse Step 1'}
            >
              {step1Collapsed ? (
                <>
                  <span>▼</span>
                  <span>Show Step 1</span>
                </>
              ) : (
                <>
                  <span>▲</span>
                  <span>Hide Step 1</span>
                </>
              )}
            </button>
          )}
        </div>
      </div>

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
      {showStep2 && templates.some(t => t.selected) && (
        <div className="mb-6 relative">
          <div className="absolute inset-0 flex items-center">
            <div className="w-full border-t border-gray-300 dark:border-gray-600"></div>
          </div>
          <div className="relative flex justify-center">
            <span className="px-4 py-1 bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 rounded-full border border-gray-300 dark:border-gray-600">
              Ready for Step 2
            </span>
          </div>
        </div>
      )}

      {/* Action Buttons - Step 2 */}
      {showStep2 && (
        <div id="step-2-infrastructure-actions" className="mb-4">
          <div className={`mb-4 ${templates.some(t => t.selected) ? 'sticky top-0 z-10 bg-white dark:bg-gray-900 pb-4 pt-2 -mt-2 border-b border-gray-200 dark:border-gray-700 mb-6 transition-all' : ''}`}>
            <div className="flex items-center justify-between mb-3">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
                Step 2: Infrastructure Actions
              </h3>
              {step1Collapsed && (
                <button
                  onClick={() => onStep1CollapsedChange(false)}
                  className="text-xs text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 font-medium"
                >
                  ← Back to Step 1
                </button>
              )}
            </div>
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-3">
              <button
                onClick={() => onAction('validate')}
                disabled={loading}
                className="px-4 py-3 bg-blue-600 dark:bg-blue-500 text-white rounded-lg hover:bg-blue-700 dark:hover:bg-blue-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                title="Validate infrastructure templates"
              >
                <span>🔍</span>
                {currentAction === 'validate' && (
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                )}
                <span>Validate</span>
              </button>
              <button
                onClick={() => onAction('preview')}
                disabled={loading || !hasValidated}
                className="px-4 py-3 bg-purple-600 dark:bg-purple-500 text-white rounded-lg hover:bg-purple-700 dark:hover:bg-purple-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                title={!hasValidated ? 'Please validate first' : 'Preview infrastructure changes'}
              >
                <span>👁️</span>
                {currentAction === 'preview' && (
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                )}
                <span>Preview</span>
              </button>
              <button
                onClick={() => onAction('deploy')}
                disabled={loading || !hasPreviewed}
                className="px-4 py-3 bg-green-600 dark:bg-green-500 text-white rounded-lg hover:bg-green-700 dark:hover:bg-green-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                title={!hasPreviewed ? 'Please preview first' : 'Deploy infrastructure'}
              >
                <span>🚀</span>
                {currentAction === 'deploy' && (
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                )}
                <span>Deploy</span>
              </button>
              <button
                onClick={() => {
                  if (whatIfChanges.length > 0 && deploymentMethod === 'azure-cli') {
                    onShowDestroySelect();
                  } else {
                    onAction('destroy');
                  }
                }}
                disabled={loading}
                className="px-4 py-3 bg-red-600 dark:bg-red-500 text-white rounded-lg hover:bg-red-700 dark:hover:bg-red-600 disabled:opacity-50 disabled:cursor-not-allowed transition-colors flex items-center justify-center gap-2"
                title="Destroy infrastructure resources"
              >
                <span>💥</span>
                {currentAction === 'destroy' && (
                  <div className="animate-spin rounded-full h-4 w-4 border-b-2 border-white"></div>
                )}
                <span>Destroy</span>
              </button>
            </div>
            <p className="text-xs text-gray-500 dark:text-gray-400 mt-3">
              Workflow: Validate → Preview → Deploy (or Destroy)
            </p>
          </div>
        </div>
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
      {whatIfChanges.length > 0 && (
        <div className="mb-8">
          <div className="mb-4 flex items-center justify-between">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-white">
              Resource Changes Preview
            </h3>
            <div className="flex gap-2">
              <button
                onClick={() => {
                  const updated = whatIfChanges.map(c => ({ ...c, selected: true }));
                  onWhatIfChangesChange(updated);
                }}
                className="px-3 py-1.5 text-xs bg-blue-100 dark:bg-blue-900 hover:bg-blue-200 dark:hover:bg-blue-800 text-blue-700 dark:text-blue-300 rounded"
              >
                Select All
              </button>
              <button
                onClick={() => {
                  const updated = whatIfChanges.map(c => ({ ...c, selected: false }));
                  onWhatIfChangesChange(updated);
                }}
                className="px-3 py-1.5 text-xs bg-gray-100 dark:bg-gray-700 hover:bg-gray-200 dark:hover:bg-gray-600 text-gray-700 dark:text-gray-300 rounded"
              >
                Deselect All
              </button>
            </div>
          </div>
          <WhatIfViewer 
            changes={whatIfChanges} 
            loading={loading}
            showSelection={hasPreviewed && deploymentMethod === 'azure-cli'}
            onSelectionChange={onWhatIfChangesChange}
            defaultResourceGroup={resourceGroupConfig.defaultResourceGroup}
            resourceGroupMappings={resourceGroupConfig.resourceTypeMappings || {}}
          />
        </div>
      )}

      {/* Deployment Info - Show when previewed but no changes (e.g., Cosmos errors only) */}
      {hasPreviewed && whatIfChanges.length === 0 && templates.some(t => t.selected) && (
        <div className="mb-8 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <h3 className="text-lg font-semibold text-blue-900 dark:text-blue-200 mb-2">
            Ready to Deploy
          </h3>
          <p className="text-sm text-blue-800 dark:text-blue-300 mb-3">
            Preview completed. The following infrastructure will be deployed based on your selected templates:
          </p>
          <div className="flex flex-wrap gap-2">
            {templates.filter(t => t.selected).map(template => (
              <span
                key={template.id}
                className="px-3 py-1.5 bg-blue-100 dark:bg-blue-900/50 text-blue-800 dark:text-blue-200 rounded-md text-sm font-medium"
              >
                {template.name}
              </span>
            ))}
          </div>
          <p className="text-xs text-blue-700 dark:text-blue-400 mt-3">
            Note: Cosmos DB nested resources (databases/containers) couldn't be previewed due to Azure limitations, but they will be deployed correctly.
          </p>
        </div>
      )}

      {/* Deployment Progress */}
      {deploymentProgress && (
        <div className="mb-6 p-4 bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg">
          <div className="flex items-center gap-3">
            <div className="animate-spin rounded-full h-5 w-5 border-b-2 border-blue-600 dark:border-blue-400"></div>
            <p className="text-sm font-medium text-blue-900 dark:text-blue-200">
              {deploymentProgress}
            </p>
          </div>
        </div>
      )}


      {/* Step 3: Project Deployment - Show after successful infrastructure deployment */}
      {hasDeployedInfrastructure && (
        <div className="mb-8">
          <div className="mb-4 relative">
            <div className="absolute inset-0 flex items-center">
              <div className="w-full border-t border-gray-300 dark:border-gray-600"></div>
            </div>
            <div className="relative flex justify-center">
              <span className="px-4 py-1 bg-gray-50 dark:bg-gray-800 text-xs font-medium text-gray-500 dark:text-gray-400 rounded-full border border-gray-300 dark:border-gray-600">
                Step 3: Deploy Projects
              </span>
            </div>
          </div>
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
      {workflowStatus && (
        <div className="bg-white dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded-lg p-6 mb-8">
          <h3 className="text-xl font-semibold text-gray-900 dark:text-white mb-4">
            Workflow Status
          </h3>

          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-4">
            <div>
              <div className="text-sm text-gray-500 dark:text-gray-400">Status</div>
              <div className="text-lg font-semibold text-gray-900 dark:text-white">
                {workflowStatus.status || 'Unknown'}
              </div>
            </div>
            <div>
              <div className="text-sm text-gray-500 dark:text-gray-400">Conclusion</div>
              <div className="text-lg font-semibold text-gray-900 dark:text-white">
                {workflowStatus.conclusion || 'N/A'}
              </div>
            </div>
            <div>
              <div className="text-sm text-gray-500 dark:text-gray-400">Workflow</div>
              <div className="text-lg font-semibold text-gray-900 dark:text-white">
                {workflowStatus.workflowName || 'N/A'}
              </div>
            </div>
            <div>
              <div className="text-sm text-gray-500 dark:text-gray-400">Updated</div>
              <div className="text-lg font-semibold text-gray-900 dark:text-white">
                {workflowStatus.updatedAt
                  ? new Date(workflowStatus.updatedAt).toLocaleTimeString()
                  : 'N/A'}
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

