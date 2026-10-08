import { Routes } from '@angular/router';
import { ErrorPageComponent } from './features/error-page/error-page.component';
import { RoundByRoundPageComponent } from './features/round-by-round-page/round-by-round-page.component';

export const routes: Routes = [
  { path: '', component: RoundByRoundPageComponent },
  {
    path: 'tournaments',
    loadComponent: () => import('./features/tournament-history/tournament-history.component')
      .then(m => m.TournamentHistoryComponent)
  },
  { path: 'tournaments/:id', component: RoundByRoundPageComponent },
  {
    path: 'tournaments/:id/battles/:battleId',
    loadComponent: () => import('./features/battle-detail/battle-detail.component')
      .then(m => m.BattleDetailComponent)
  },
  // The original instant-results page (Quick Tournament), reached from the footer.
  {
    path: 'classic',
    loadComponent: () => import('./features/tournament-page/tournament-page.component')
      .then(m => m.TournamentPageComponent)
  },
  { path: 'error', component: ErrorPageComponent },
  { path: '**', redirectTo: '' }
];
