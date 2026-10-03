import { Routes } from '@angular/router';

export const routes: Routes = [
  { path: '', redirectTo: 'workflows', pathMatch: 'full' },
  {
    path: 'workflows',
    loadComponent: () =>
      import('./workflows-list/workflows-list').then(m => m.WorkflowsList)
  },
  {
    path: 'runs',
    loadComponent: () =>
      import('./runs-list/runs-list').then(m => m.RunsList)
  },
  {
    path: 'runs/:id',
    loadComponent: () =>
      import('./run-detail/run-detail').then(m => m.RunDetail)
  },
  {
    path: 'agents',
    loadComponent: () =>
      import('./agents-list/agents-list').then(m => m.AgentsList)
  },
  { path: '**', redirectTo: 'workflows' }
];