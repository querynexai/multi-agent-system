import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { environment } from '../environments/environment';
import {
  AgentRecord,
  AgentTypes,
  ApprovalResult,
  RunCreated,
  RunDetail,
  WorkflowDefinition,
  WorkflowRecord,
  WorkflowRun
} from './models';

@Injectable({ providedIn: 'root' })
export class AgentFlowService {
  private readonly http = inject(HttpClient);
  private readonly base = environment.apiBase;

  // ---------- Workflows ----------
  workflows() {
    return this.http.get<WorkflowRecord[]>(`${this.base}/workflows`);
  }

  workflow(id: string) {
    return this.http.get<WorkflowRecord>(`${this.base}/workflows/${id}`);
  }

  saveWorkflow(def: WorkflowDefinition) {
    return this.http.post<WorkflowDefinition>(`${this.base}/workflows`, def);
  }

  runWorkflow(id: string, input: Record<string, unknown> = {}) {
    return this.http.post<RunCreated>(`${this.base}/workflows/${id}/run`, { input });
  }

  // ---------- Runs ----------
  runs(take = 50) {
    return this.http.get<WorkflowRun[]>(`${this.base}/runs?take=${take}`);
  }

  run(id: string) {
    return this.http.get<RunDetail>(`${this.base}/runs/${id}`);
  }

  approve(runId: string, stepId: string, approved: boolean, comment = '') {
    return this.http.post<ApprovalResult>(
      `${this.base}/runs/${runId}/steps/${stepId}/approve`,
      { approved, comment }
    );
  }

  // ---------- Agents ----------
  agents() {
    return this.http.get<AgentRecord[]>(`${this.base}/agents`);
  }

  agentTypes() {
    return this.http.get<AgentTypes>(`${this.base}/agents/types`);
  }
}