import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { Round, Standing, Tournament } from '../../models/round-by-round.model';
import { IndividualCardsComponent } from '../individual-cards/individual-cards.component';
import { getReasonLabel, getRoundResults, RoundResult } from '../../helpers/round-by-round.helper';
import { getURL } from '../../helpers/tournament.helper';

/**
 * Home page and live/review view of one Tournament (/tournaments/:id).
 * Without an id it only offers to start a Tournament.
 */
@Component({
  selector: 'app-round-by-round-page',
  standalone: true,
  imports: [CommonModule, RouterLink, IndividualCardsComponent],
  templateUrl: './round-by-round-page.component.html',
  styleUrls: ['../round-by-round.shared.css', './round-by-round-page.component.css']
})
export class RoundByRoundPageComponent implements OnInit {

  tournament: Tournament | null = null;
  selectedRoundNumber: number | null = null;

  isLoading = false;
  isStarting = false;
  isProcessing = false;
  notAvailable = false;

  readonly getReasonLabel = getReasonLabel;
  readonly getPokemonImage = getURL;

  constructor(
    private roundByRoundService: RoundByRoundService,
    private route: ActivatedRoute,
    private router: Router) {}

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    const round = Number(this.route.snapshot.queryParamMap.get('round'));

    if (id) {
      this.load(id, round || null);
    }
  }

  get nextRound(): Round | null {
    return this.tournament?.rounds.find(r => r.status === 'Pending') ?? null;
  }

  get selectedRound(): Round | null {
    return this.tournament?.rounds.find(r => r.number === this.selectedRoundNumber) ?? null;
  }

  get isCompleted(): boolean {
    return this.tournament?.status === 'Completed';
  }

  /** Standings as of the selected processed Round, otherwise the current ones. */
  get visibleStandings(): Standing[] {
    const round = this.selectedRound;
    return round?.status === 'Processed' && round.standings
      ? round.standings
      : this.tournament?.standings ?? [];
  }

  get roundResults(): Map<number, RoundResult> {
    const round = this.selectedRound;
    return round?.status === 'Processed' ? getRoundResults(round) : new Map();
  }

  get isReviewingPastRound(): boolean {
    const round = this.selectedRound;
    return !!round && round.status === 'Processed' && round.number < (this.tournament?.roundsProcessed ?? 0);
  }

  get leaders(): Standing[] {
    return this.isCompleted ? this.visibleStandings.filter(s => s.rank === 1) : [];
  }

  canSelect(round: Round): boolean {
    return round.status === 'Processed' || round === this.nextRound;
  }

  selectRound(round: Round): void {
    if (this.canSelect(round)) {
      this.selectedRoundNumber = round.number;
    }
  }

  showLatest(): void {
    this.selectedRoundNumber = this.defaultRoundNumber();
  }

  start(): void {
    this.isStarting = true;

    this.roundByRoundService.startTournament().subscribe({
      next: (tournament) => {
        this.isStarting = false;
        this.router.navigate(['/tournaments', tournament.id]);
      },
      error: (error) => {
        this.isStarting = false;
        this.handleError(error, '/');
      }
    });
  }

  processRound(): void {
    const tournament = this.tournament;
    if (!tournament || this.isProcessing || this.isCompleted) {
      return;
    }

    this.isProcessing = true;

    this.roundByRoundService.processRound(tournament.id, tournament.roundsProcessed + 1).subscribe({
      next: (response) => {
        this.tournament = {
          ...tournament,
          status: response.status,
          roundsProcessed: response.roundsProcessed,
          standings: response.standings,
          rounds: tournament.rounds.map(r => r.number === response.round.number ? response.round : r)
        };
        this.selectedRoundNumber = response.round.number;
        this.isProcessing = false;
      },
      error: (error: HttpErrorResponse) => {
        this.isProcessing = false;

        if (error.status === 409) {
          // Already processed (e.g. in another tab): catch up with the server.
          this.load(tournament.id, null);
          return;
        }

        this.handleError(error, `/tournaments/${tournament.id}`);
      }
    });
  }

  private load(id: string, preferredRound: number | null): void {
    this.isLoading = true;
    this.notAvailable = false;

    this.roundByRoundService.getTournament(id).subscribe({
      next: (tournament) => {
        this.tournament = tournament;
        const preferred = tournament.rounds.find(r => r.number === preferredRound);
        this.selectedRoundNumber = preferred && this.canSelect(preferred)
          ? preferred.number
          : this.defaultRoundNumber();
        this.isLoading = false;
      },
      error: (error) => {
        this.isLoading = false;
        this.handleError(error, `/tournaments/${id}`);
      }
    });
  }

  /** Latest processed Round, or the first Round's preview before any is played. */
  private defaultRoundNumber(): number | null {
    const tournament = this.tournament;
    if (!tournament) {
      return null;
    }

    return tournament.roundsProcessed > 0
      ? tournament.roundsProcessed
      : this.nextRound?.number ?? null;
  }

  private handleError(error: HttpErrorResponse, returnUrl: string): void {
    if (error.status === 404) {
      this.notAvailable = true;
      this.tournament = null;
      return;
    }

    console.error('Round-by-round tournament request failed.', error);
    this.router.navigate(['/error'], { state: { returnUrl } });
  }
}
