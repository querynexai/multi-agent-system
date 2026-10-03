import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { Router } from '@angular/router';
import { AgentFlowService } from '../agent-flow.service';
import { WorkflowRecord } from '../models';

@Component({
  selector: 'app-workflows-list',
  imports: [],
  templateUrl: './workflows-list.html',
  styleUrl: './workflows-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkflowsList implements OnInit {
  private readonly api = inject(AgentFlowService);
  private readonly router = inject(Router);

  protected readonly workflows = signal<WorkflowRecord[]>([]);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly running = signal<string | null>(null);

  ngOnInit(): void {
    this.reload();
  }

  protected reload(): void {
    this.loading.set(true);
    this.error.set(null);
    this.api.workflows().subscribe({
      next: w => {
        this.workflows.set(w);
        this.loading.set(false);
      },
      error: (err: Error) => {
        this.error.set(err?.message ?? 'Unknown error');
        this.loading.set(false);
      }
    });
  }

  protected run(id: string): void {
    this.running.set(id);
    this.api.runWorkflow(id, { requestedBy: 'dashboard' }).subscribe({
      next: r => this.router.navigate(['/runs', r.runId]),
      error: () => this.running.set(null)
    });
  }
}