import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnDestroy,
  OnInit,
  signal
} from '@angular/core';
import { DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { AgentFlowService } from '../agent-flow.service';
import { WorkflowRun } from '../models';

@Component({
  selector: 'app-runs-list',
  imports: [DatePipe, RouterLink],
  templateUrl: './runs-list.html',
  styleUrl: './runs-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class RunsList implements OnInit, OnDestroy {
  private readonly api = inject(AgentFlowService);

  protected readonly runs = signal<WorkflowRun[]>([]);
  private timer?: ReturnType<typeof setInterval>;

  ngOnInit(): void {
    this.refresh();
    this.timer = setInterval(() => this.refresh(), 3000);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
  }

  protected refresh(): void {
    this.api.runs().subscribe(r => this.runs.set(r));
  }
}