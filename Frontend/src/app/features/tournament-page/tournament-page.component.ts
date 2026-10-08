import { Component, OnInit } from '@angular/core';
import { TournamentService } from '../../services/tournament.service';
import { Pokemon, SortDirection, SortOptions } from '../../models/pokemon.model';
import { getURL, getWinRate } from '../../helpers/tournament.helper';
import { FormsModule } from '@angular/forms';
import { IndividualCardsComponent } from '../individual-cards/individual-cards.component';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';

@Component({
  selector: 'app-tournament-page',
  imports: [FormsModule, IndividualCardsComponent, CommonModule],
  standalone: true,
  templateUrl: './tournament-page.component.html',
  styleUrls: ['./tournament-page.component.css']
})
export class TournamentPageComponent implements OnInit {

  total: Pokemon[] = [];
  pagedTotal: Pokemon[] = [];

  sortBy: SortOptions = SortOptions.Wins;
  sortDirection: SortDirection = SortDirection.Asc;

  sortOptionsList = Object.values(SortOptions);
  sortDirectionList = Object.values(SortDirection);

  pageSize = 8;
  currentPage = 1;

  errorMessage: string | null = null;
  isLoading = false;

  directionLabels: Record<string, string> = {
  asc: 'Ascending',
  desc: 'Descending'
};

  get battleSpotlight(): Pokemon[] {
    return [...this.total]
      .sort((first, second) =>
        getWinRate(second.wins, second.losses, second.ties) -
        getWinRate(first.wins, first.losses, first.ties))
      .slice(0, 2);
  }

  getPokemonImage(id: number): string {
    return getURL(id);
  }


  constructor(
    private tournamentService: TournamentService,
    private router: Router) {}

  ngOnInit(): void {
    this.loadData();
  }

  loadData(): void {
    this.errorMessage = null;
    this.isLoading = true;

    this.tournamentService.getTournamentStatistics(
      this.sortBy,
      this.sortDirection)
      .subscribe({
        next: (data) => {
          this.total = data;
          this.currentPage = 1;
          this.updatePage();
          this.isLoading = false;
        },
        error: (error) => {
          this.errorMessage = 'Unable to load tournament statistics.';
          this.isLoading = false;
          console.error('Tournament statistics request failed.', error);
          this.router.navigate(['/error'], {
            state: { returnUrl: '/classic' }
          });
        }
      });
  }

  
  changeSort(): void {
    this.loadData();
  }

 updatePage(): void {
  const start = (this.currentPage - 1) * this.pageSize;
  this.pagedTotal = this.total.slice(start, start + this.pageSize);
}

changePageSize(): void {
  this.currentPage = 1;
  this.updatePage();
}

nextPage(): void {
  const totalPages = Math.ceil(this.total.length / this.pageSize);
  if (this.currentPage < totalPages) {
    this.currentPage++;
    this.updatePage();
  }
}

prevPage(): void {
  if (this.currentPage > 1) {
    this.currentPage--;
    this.updatePage();
  }
}

}
