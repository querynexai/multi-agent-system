export interface WorkflowRecord {
  id: string;
  name: string;
  description?: string;
  definition: WorkflowDefinition;
  createdAt: string;
  updatedAt: string;
}

export interface WorkflowDefinition {
  id: string;
  name: string;
  description?: string;
  steps: StepDefinition[];
}

export interface StepDefinition {
  id: string;
  name: string;
  agent?: string;
  type?: string;
  dependsOn?: string[];
  input?: unknown;
  retry?: { maxAttempts: number; baseDelayMs: number };
  timeoutSeconds?: number;
  approvers?: string[];
  when?: string;
}

export interface WorkflowRun {
  id: string;
  workflowId: string;
  status: string;
  input?: unknown;
  context: unknown;
  error?: string;
  startedAt: string;
  finishedAt?: string;
}

export interface StepRun {
  id: string;
  runId: string;
  stepId: string;
  name: string;
  type: string;
  status: string;
  attempt: number;
  input?: unknown;
  output?: unknown;
  error?: string;
  logs?: string;
  startedAt?: string;
  finishedAt?: string;
}

export interface AgentRecord {
  id: string;
  name: string;
  type: string;
  description?: string;
  config: Record<string, unknown>;
  createdAt: string;
}

export interface RunDetail {
  run: WorkflowRun;
  steps: StepRun[];
}

export interface RunCreated {
  runId: string;
}

export interface AgentTypes {
  implementations: string[];
}

export interface ApprovalResult {
  resumed: boolean;
}