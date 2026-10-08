import { Component, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { TournamentSummary } from '../../models/round-by-round.model';

/** Tournament History (/tournaments): every Tournament run so far, newest first. */
@Component({
  selector: 'app-tournament-history',
  standalone: true,
  imports: [DatePipe, RouterLink],
  templateUrl: './tournament-history.component.html',
  styleUrls: ['../round-by-round.shared.css', './tournament-history.component.css']
})
export class TournamentHistoryComponent implements OnInit {

  history: TournamentSummary[] = [];
  isLoading = false;
  isStarting = false;

  constructor(
    private roundByRoundService: RoundByRoundService,
    private router: Router) {}

  ngOnInit(): void {
    this.isLoading = true;

    this.roundByRoundService.getHistory().subscribe({
      next: (history) => {
        this.history = history;
        this.isLoading = false;
      },
      error: (error) => this.fail(error, '/tournaments')
    });
  }

  start(): void {
    this.isStarting = true;

    this.roundByRoundService.startTournament().subscribe({
      next: (tournament) => {
        this.isStarting = false;
        this.router.navigate(['/tournaments', tournament.id]);
      },
      error: (error) => this.fail(error, '/tournaments')
    });
  }

  leaderNames(summary: TournamentSummary): string {
    return summary.leaders.map(l => l.name).join(', ');
  }

  private fail(error: unknown, returnUrl: string): void {
    this.isLoading = false;
    this.isStarting = false;
    console.error('Tournament history request failed.', error);
    this.router.navigate(['/error'], { state: { returnUrl } });
  }
}
