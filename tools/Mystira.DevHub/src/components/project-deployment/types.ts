export interface DeploymentStatus {
  projectId: string;
  resources: {
    storage: { deployed: boolean; name?: string };
    cosmos: { deployed: boolean; name?: string };
    appService: { deployed: boolean; name?: string };
    keyVault: { deployed: boolean; name?: string };
  };
  allRequiredDeployed: boolean;
  lastChecked: number | null;
}

export interface ProjectPipeline {
  projectId: string;
  workflowFile: string;
  environment: string;
  inputs?: Record<string, string>;
}

export interface WorkflowRun {
  id: number;
  name: string;
  status: 'queued' | 'in_progress' | 'completed' | 'cancelled';
  conclusion: 'success' | 'failure' | 'cancelled' | null;
  html_url: string;
  created_at: string;
  updated_at: string;
}
