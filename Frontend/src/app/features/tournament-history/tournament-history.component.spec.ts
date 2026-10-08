/// <reference types="jasmine" />

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { TournamentHistoryComponent } from './tournament-history.component';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { Tournament, TournamentSummary } from '../../models/round-by-round.model';

describe('TournamentHistoryComponent', () => {
  let fixture: ComponentFixture<TournamentHistoryComponent>;
  let service: jasmine.SpyObj<RoundByRoundService>;

  const history: TournamentSummary[] = [
    {
      id: 'new', startedAt: '2026-10-08T13:00:00Z', status: 'InProgress', roundsProcessed: 0, totalRounds: 15,
      leaders: []
    },
    {
      id: 'old', startedAt: '2026-10-08T12:00:00Z', status: 'Completed', roundsProcessed: 15, totalRounds: 15,
      leaders: [
        { rank: 1, id: 6, name: 'charizard', type: 'fire', wins: 12, losses: 3, ties: 0 },
        { rank: 1, id: 9, name: 'blastoise', type: 'water', wins: 12, losses: 3, ties: 0 }
      ]
    }
  ];

  beforeEach(async () => {
    service = jasmine.createSpyObj<RoundByRoundService>('RoundByRoundService', ['getHistory', 'startTournament']);
    service.getHistory.and.returnValue(of(history));

    await TestBed.configureTestingModule({
      imports: [TournamentHistoryComponent],
      providers: [provideRouter([]), { provide: RoundByRoundService, useValue: service }]
    }).compileComponents();

    fixture = TestBed.createComponent(TournamentHistoryComponent);
    fixture.detectChanges();
  });

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  it('lists past tournaments linking to each one', () => {
    const rows = fixture.nativeElement.querySelectorAll('.history-row') as NodeListOf<HTMLAnchorElement>;

    expect(rows.length).toBe(2);
    expect(rows[0].getAttribute('href')).toBe('/tournaments/new');
    expect(text()).toContain('0/15');
    expect(text()).toContain('Not started');
    expect(text()).toContain('charizard, blastoise');
  });

  it('starts a new tournament and opens it', () => {
    const router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    service.startTournament.and.returnValue(of({ id: 'fresh' } as Tournament));

    fixture.componentInstance.start();

    expect(router.navigate).toHaveBeenCalledWith(['/tournaments', 'fresh']);
  });
});
