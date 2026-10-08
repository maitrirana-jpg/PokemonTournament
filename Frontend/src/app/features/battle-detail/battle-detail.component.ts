import { Component, OnInit } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { Battle, Participant } from '../../models/round-by-round.model';
import { getReasonLabel } from '../../helpers/round-by-round.helper';
import { getTypeBadgeClass, getURL } from '../../helpers/tournament.helper';

/** Review of one Battle (/tournaments/:id/battles/:battleId). */
@Component({
  selector: 'app-battle-detail',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './battle-detail.component.html',
  styleUrls: ['../round-by-round.shared.css', './battle-detail.component.css']
})
export class BattleDetailComponent implements OnInit {

  tournamentId = '';
  battle: Battle | null = null;
  isLoading = false;
  notAvailable = false;

  readonly getPokemonImage = getURL;
  readonly getTypeBadgeClass = getTypeBadgeClass;

  constructor(
    private roundByRoundService: RoundByRoundService,
    private route: ActivatedRoute,
    private router: Router) {}

  ngOnInit(): void {
    this.tournamentId = this.route.snapshot.paramMap.get('id') ?? '';
    const battleId = Number(this.route.snapshot.paramMap.get('battleId'));
    this.isLoading = true;

    this.roundByRoundService.getBattle(this.tournamentId, battleId).subscribe({
      next: (battle) => {
        this.battle = battle;
        this.isLoading = false;
      },
      error: (error: HttpErrorResponse) => {
        this.isLoading = false;

        if (error.status === 404) {
          this.notAvailable = true;
          return;
        }

        console.error('Battle request failed.', error);
        this.router.navigate(['/error'], {
          state: { returnUrl: `/tournaments/${this.tournamentId}/battles/${battleId}` }
        });
      }
    });
  }

  get verdict(): string {
    const battle = this.battle;
    if (!battle || battle.status === 'Pending') {
      return 'Not fought yet';
    }

    if (battle.winnerId === null) {
      return 'It\'s a tie';
    }

    const winner = battle.winnerId === battle.first.id ? battle.first : battle.second;
    return `${winner.name.charAt(0).toUpperCase()}${winner.name.slice(1)} wins`;
  }

  get reasonLabel(): string {
    return getReasonLabel(this.battle?.reason ?? null);
  }

  isWinner(participant: Participant): boolean {
    return this.battle?.winnerId === participant.id;
  }
}
