import { useEffect, useRef, useState } from 'react';
import { invoke } from '@tauri-apps/api/tauri';
import type { CommandResponse } from '../../../types';
import type { ProjectInfo } from '../../ProjectDeploymentPlanner';
import type { ProjectPipeline, WorkflowRun } from '../types';

interface UseProjectDeploymentProps {
  environment: string;
  projects: ProjectInfo[];
}

export function useProjectDeployment({ environment, projects }: UseProjectDeploymentProps) {
  const [selectedProjects, setSelectedProjects] = useState<Set<string>>(new Set());
  const [projectPipelines, setProjectPipelines] = useState<Record<string, ProjectPipeline>>({});
  const [deploying, setDeploying] = useState(false);
  const [workflowRuns, setWorkflowRuns] = useState<Record<string, WorkflowRun>>({});
  const [workflowLogs, setWorkflowLogs] = useState<Record<string, string[]>>({});
  const [showLogs, setShowLogs] = useState<Record<string, boolean>>({});
  const [availableWorkflows, setAvailableWorkflows] = useState<string[]>([]);
  const logsEndRefs = useRef<Record<string, HTMLDivElement | null>>({});

  useEffect(() => {
    loadAvailableWorkflows();
  }, [environment]);

  const loadAvailableWorkflows = async () => {
    try {
      const response = await invoke<CommandResponse<string[]>>('list_github_workflows', {
        environment,
      });
      
      if (response.success && response.result) {
        setAvailableWorkflows(response.result);
      } else {
        console.warn('Workflow discovery failed, using fallback list:', response.error);
        const workflows = [
          `mystira-app-api-cicd-${environment}.yml`,
          `mystira-app-admin-api-cicd-${environment}.yml`,
          `mystira-app-pwa-cicd-${environment}.yml`,
          `infrastructure-deploy-${environment}.yml`,
        ];
        setAvailableWorkflows(workflows);
      }
    } catch (error) {
      console.error('Failed to load workflows:', error);
      const workflows = [
        `mystira-app-api-cicd-${environment}.yml`,
        `mystira-app-admin-api-cicd-${environment}.yml`,
        `mystira-app-pwa-cicd-${environment}.yml`,
        `infrastructure-deploy-${environment}.yml`,
      ];
      setAvailableWorkflows(workflows);
    }
  };

  useEffect(() => {
    const saved = localStorage.getItem(`projectPipelines_${environment}`);
    if (saved) {
      try {
        const parsed = JSON.parse(saved);
        setProjectPipelines(parsed);
      } catch (e) {
        console.error('Failed to parse saved pipelines:', e);
      }
    } else {
      const defaults: Record<string, ProjectPipeline> = {};
      projects.forEach(project => {
        if (project.id === 'mystira-api') {
          defaults[project.id] = {
            projectId: project.id,
            workflowFile: `mystira-app-api-cicd-${environment}.yml`,
            environment,
          };
        } else if (project.id === 'mystira-admin-api') {
          defaults[project.id] = {
            projectId: project.id,
            workflowFile: `mystira-app-admin-api-cicd-${environment}.yml`,
            environment,
          };
        } else if (project.id === 'mystira-pwa') {
          defaults[project.id] = {
            projectId: project.id,
            workflowFile: `mystira-app-pwa-cicd-${environment}.yml`,
            environment,
          };
        }
      });
      setProjectPipelines(defaults);
    }
  }, [environment, projects]);

  useEffect(() => {
    localStorage.setItem(`projectPipelines_${environment}`, JSON.stringify(projectPipelines));
  }, [projectPipelines, environment]);

  const toggleProjectSelection = (projectId: string) => {
    const newSelected = new Set(selectedProjects);
    if (newSelected.has(projectId)) {
      newSelected.delete(projectId);
    } else {
      newSelected.add(projectId);
    }
    setSelectedProjects(newSelected);
  };

  const updatePipeline = (projectId: string, workflowFile: string) => {
    setProjectPipelines(prev => ({
      ...prev,
      [projectId]: {
        ...prev[projectId],
        projectId,
        workflowFile,
        environment,
      },
    }));
  };

  const handleDeployProjects = async () => {
    if (selectedProjects.size === 0) {
      alert('Please select at least one project to deploy');
      return;
    }

    setDeploying(true);
    const newWorkflowRuns: Record<string, WorkflowRun> = {};
    const newWorkflowLogs: Record<string, string[]> = {};

    try {
      for (const projectId of selectedProjects) {
        const pipeline = projectPipelines[projectId];
        if (!pipeline) {
          console.error(`No pipeline configured for project ${projectId}`);
          continue;
        }

        const dispatchResponse = await invoke<CommandResponse>('github_dispatch_workflow', {
          workflowFile: pipeline.workflowFile,
          inputs: pipeline.inputs || {},
        });

        if (dispatchResponse.success && dispatchResponse.result) {
          const run = dispatchResponse.result as any;
          newWorkflowRuns[projectId] = {
            id: run.id,
            name: run.name || pipeline.workflowFile,
            status: 'queued',
            conclusion: null,
            html_url: run.html_url || '',
            created_at: run.created_at || new Date().toISOString(),
            updated_at: run.updated_at || new Date().toISOString(),
          };
          newWorkflowLogs[projectId] = [`🚀 Dispatched workflow: ${pipeline.workflowFile}`];
          setShowLogs(prev => ({ ...prev, [projectId]: true }));
          
          const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
          localStorage.setItem(lastDeployedKey, Date.now().toString());
        } else {
          newWorkflowLogs[projectId] = [`❌ Failed to dispatch: ${dispatchResponse.error || 'Unknown error'}`];
        }
      }

      setWorkflowRuns(newWorkflowRuns);
      setWorkflowLogs(newWorkflowLogs);

      if (Object.keys(newWorkflowRuns).length > 0) {
        pollWorkflowStatus(Object.keys(newWorkflowRuns));
      }
    } catch (error) {
      console.error('Failed to deploy projects:', error);
    } finally {
      setDeploying(false);
    }
  };

  const pollWorkflowStatus = async (projectIds: string[]) => {
    const interval = setInterval(async () => {
      let allCompleted = true;

      for (const projectId of projectIds) {
        const run = workflowRuns[projectId];
        if (!run || (run.status === 'completed' || run.status === 'cancelled')) {
          continue;
        }

        allCompleted = false;

        try {
          const statusResponse = await invoke<CommandResponse>('github_workflow_status', {
            runId: run.id,
          });

          if (statusResponse.success && statusResponse.result) {
            const status = statusResponse.result as any;
            setWorkflowRuns(prev => ({
              ...prev,
              [projectId]: {
                ...prev[projectId],
                status: status.status,
                conclusion: status.conclusion,
                updated_at: status.updated_at || prev[projectId].updated_at,
              },
            }));
            
            if (status.status === 'completed' && status.conclusion === 'success') {
              const lastDeployedKey = `lastDeployed_${projectId}_${environment}`;
              localStorage.setItem(lastDeployedKey, Date.now().toString());
            }

            if (status.status === 'in_progress' || status.status === 'queued') {
              const logsResponse = await invoke<CommandResponse>('github_workflow_logs', {
                runId: run.id,
              });

              if (logsResponse.success && logsResponse.result) {
                const logs = logsResponse.result as string[];
                setWorkflowLogs(prev => ({
                  ...prev,
                  [projectId]: logs,
                }));
              }
            }
          }
        } catch (error) {
          console.error(`Failed to get status for ${projectId}:`, error);
        }
      }

      if (allCompleted) {
        clearInterval(interval);
      }
    }, 3000);

    setTimeout(() => clearInterval(interval), 600000);
  };

  return {
    selectedProjects,
    projectPipelines,
    deploying,
    workflowRuns,
    workflowLogs,
    showLogs,
    availableWorkflows,
    logsEndRefs,
    toggleProjectSelection,
    updatePipeline,
    handleDeployProjects,
    setShowLogs,
  };
}

