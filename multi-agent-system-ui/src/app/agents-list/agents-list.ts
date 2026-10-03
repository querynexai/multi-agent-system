import {
  ChangeDetectionStrategy,
  Component,
  inject,
  OnInit,
  signal
} from '@angular/core';
import { JsonPipe } from '@angular/common';
import { AgentFlowService } from '../agent-flow.service';
import { AgentRecord } from '../models';

@Component({
  selector: 'app-agents-list',
  imports: [JsonPipe],
  templateUrl: './agents-list.html',
  styleUrl: './agents-list.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class AgentsList implements OnInit {
  private readonly api = inject(AgentFlowService);

  protected readonly agents = signal<AgentRecord[]>([]);
  protected readonly types = signal<string[]>([]);

  ngOnInit(): void {
    this.api.agents().subscribe(a => this.agents.set(a));
    this.api.agentTypes().subscribe(t => this.types.set(t.implementations));
  }

  protected hasConfig(cfg: Record<string, unknown> | undefined): boolean {
    return !!cfg && Object.keys(cfg).length > 0;
  }
}