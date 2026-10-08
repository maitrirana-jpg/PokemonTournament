import { Component, Input } from '@angular/core';
import { Pokemon } from '../../models/pokemon.model';
import { getTypeBadgeClass, getURL, getWinRate } from '../../helpers/tournament.helper';
import { RoundResult } from '../../helpers/round-by-round.helper';

@Component({
  selector: 'app-individual-cards',
  standalone: true,
  imports: [],
  templateUrl: './individual-cards.component.html',
  styleUrl: './individual-cards.component.css'
})
export class IndividualCardsComponent {
 @Input({ required: true }) pokemon!: Pokemon;

    // Round-by-round view only; the classic view leaves these unset.
    @Input() rank?: number;
    @Input() lastResult?: RoundResult | null;

    readonly resultLabels: Record<RoundResult, string> = { W: 'Won', L: 'Lost', T: 'Tied' };

    get Url(): string {
        return getURL(this.pokemon.id);
    }

    get typeBadgeClass(): string {
        return getTypeBadgeClass(this.pokemon.type);
    }

    get winRate(): number {
        return getWinRate(this.pokemon.wins, this.pokemon.losses, this.pokemon.ties);
    }
}
