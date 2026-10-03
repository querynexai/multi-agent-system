import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import { DatePipe, JsonPipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AgentFlowService } from '../agent-flow.service';
import { StepRun, WorkflowRun } from '../models';

@Component({
  selector: 'app-run-detail',
  imports: [DatePipe, JsonPipe, RouterLink],
  templateUrl: './run-detail.html',
  styleUrl: './run-detail.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RunDetail implements OnInit, OnDestroy {
  private readonly api = inject(AgentFlowService);
  private readonly route = inject(ActivatedRoute);

  protected readonly run = signal<WorkflowRun | null>(null);
  protected readonly steps = signal<StepRun[]>([]);

  private timer?: ReturnType<typeof setInterval>;
  private id!: string;

  ngOnInit(): void {
    this.id = this.route.snapshot.paramMap.get('id')!;
    this.refresh();
    this.timer = setInterval(() => this.refresh(), 2000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  protected refresh(): void {
    this.api.run(this.id).subscribe(({ run, steps }) => {
      this.run.set(run);
      this.steps.set(steps);
    });
  }

  protected decide(stepId: string, approved: boolean): void {
    const comment = approved ? 'Looks good' : 'Needs revision';
    this.api.approve(this.id, stepId, approved, comment)
      .subscribe(() => this.refresh());
  }
}