/// <reference types="jasmine" />

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { RoundByRoundPageComponent } from './round-by-round-page.component';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { Battle, Participant, Round, Standing, Tournament } from '../../models/round-by-round.model';

const participants: Participant[] = [1, 2, 3, 4].map(id => ({
  id, name: `pokemon-${id}`, type: 'normal', baseExperience: 100 + id
}));

const standing = (p: Participant, rank: number, wins = 0, losses = 0): Standing =>
  ({ rank, id: p.id, name: p.name, type: p.type, wins, losses, ties: 0 });

const evenStandings = participants.map(p => standing(p, 1));

const pendingRound = (number: number): Round => ({
  number,
  status: 'Pending',
  standings: null,
  battles: [
    { first: participants[0], second: participants[3] },
    { first: participants[1], second: participants[2] }
  ].map((pair, index): Battle => ({
    id: (number - 1) * 2 + index + 1, roundNumber: number, status: 'Pending',
    ...pair, outcome: null, winnerId: null, reason: null
  }))
});

/** Round where the first Participant of each Battle wins. */
const processedRound = (number: number): Round => {
  const round = pendingRound(number);
  return {
    ...round,
    status: 'Processed',
    battles: round.battles.map(b => ({
      ...b, status: 'Processed', outcome: 'FirstWins', winnerId: b.first.id, reason: 'BaseExperience'
    })),
    standings: [
      standing(participants[0], 1, number), standing(participants[1], 1, number),
      standing(participants[2], 3, 0, number), standing(participants[3], 3, 0, number)
    ]
  };
};

const newTournament = (): Tournament => ({
  id: 'abc', startedAt: '2026-10-08T12:00:00Z', status: 'InProgress',
  roundsProcessed: 0, totalRounds: 3, participants,
  rounds: [1, 2, 3].map(pendingRound), standings: evenStandings
});

describe('RoundByRoundPageComponent', () => {
  let fixture: ComponentFixture<RoundByRoundPageComponent>;
  let component: RoundByRoundPageComponent;
  let service: jasmine.SpyObj<RoundByRoundService>;
  let router: Router;

  async function create(id: string | null, round: string | null = null): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [RoundByRoundPageComponent],
      providers: [
        provideRouter([]),
        { provide: RoundByRoundService, useValue: service },
        {
          provide: ActivatedRoute,
          useValue: {
            snapshot: {
              paramMap: convertToParamMap(id ? { id } : {}),
              queryParamMap: convertToParamMap(round ? { round } : {})
            }
          }
        }
      ]
    }).compileComponents();

    router = TestBed.inject(Router);
    spyOn(router, 'navigate').and.resolveTo(true);
    fixture = TestBed.createComponent(RoundByRoundPageComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';
  const query = (selector: string) => (fixture.nativeElement as HTMLElement).querySelectorAll(selector);

  beforeEach(() => {
    service = jasmine.createSpyObj<RoundByRoundService>('RoundByRoundService',
      ['startTournament', 'getTournament', 'processRound']);
    service.getTournament.and.returnValue(of(newTournament()));
  });

  describe('without a tournament', () => {
    beforeEach(() => create(null));

    it('offers a Start Tournament button', () => {
      expect(text()).toContain('Start Tournament');
      expect(service.getTournament).not.toHaveBeenCalled();
    });

    it('starts a tournament and opens it', () => {
      service.startTournament.and.returnValue(of(newTournament()));

      component.start();

      expect(service.startTournament).toHaveBeenCalled();
      expect(router.navigate).toHaveBeenCalledWith(['/tournaments', 'abc']);
    });
  });

  describe('with a new tournament', () => {
    beforeEach(() => create('abc'));

    it('shows every participant even, with the first round coming up', () => {
      expect(service.getTournament).toHaveBeenCalledWith('abc');
      expect(query('app-individual-cards').length).toBe(4);
      expect(component.visibleStandings.every(s => s.wins + s.losses + s.ties === 0)).toBeTrue();
      expect(component.selectedRound?.number).toBe(1);
      expect(text()).toContain('Coming up');
    });

    it('processes the next round and reveals its results and standings', () => {
      const round1 = processedRound(1);
      service.processRound.and.returnValue(of({
        round: round1, standings: round1.standings!, status: 'InProgress', roundsProcessed: 1, totalRounds: 3
      }));

      component.processRound();
      fixture.detectChanges();

      expect(service.processRound).toHaveBeenCalledWith('abc', 1);
      expect(component.tournament?.roundsProcessed).toBe(1);
      expect(component.selectedRound?.status).toBe('Processed');
      expect(component.visibleStandings[0].wins).toBe(1);
      expect(component.roundResults.get(1)).toBe('W');
      expect(query('.battle-row a').length).toBe(2);
    });

    it('reloads when the round was already processed elsewhere (409)', () => {
      service.processRound.and.returnValue(throwError(() => new HttpErrorResponse({ status: 409 })));

      component.processRound();

      expect(service.getTournament).toHaveBeenCalledTimes(2);
    });

    it('shows "no longer available" when the tournament vanished (404)', () => {
      service.processRound.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));

      component.processRound();
      fixture.detectChanges();

      expect(component.notAvailable).toBeTrue();
      expect(text()).toContain('This tournament is no longer available');
    });

    it('only lets processed rounds and the next round be selected', () => {
      expect(component.canSelect(component.tournament!.rounds[0])).toBeTrue();
      expect(component.canSelect(component.tournament!.rounds[1])).toBeFalse();
    });
  });

  describe('reviewing', () => {
    it('shows standings as of an earlier round when it is selected', async () => {
      service.getTournament.and.returnValue(of({
        ...newTournament(),
        roundsProcessed: 2,
        rounds: [processedRound(1), processedRound(2), pendingRound(3)],
        standings: processedRound(2).standings!
      }));
      await create('abc', '1');

      expect(component.selectedRound?.number).toBe(1);
      expect(component.visibleStandings[0].wins).toBe(1);
      expect(component.isReviewingPastRound).toBeTrue();
    });

    it('disables Process Round and shows the leaders once completed', async () => {
      const finalStandings = processedRound(3).standings!;
      service.getTournament.and.returnValue(of({
        ...newTournament(),
        status: 'Completed',
        roundsProcessed: 3,
        rounds: [processedRound(1), processedRound(2), processedRound(3)],
        standings: finalStandings
      }));
      await create('abc');

      const button = fixture.nativeElement.querySelector('.process-button') as HTMLButtonElement;
      expect(button.disabled).toBeTrue();
      expect(component.leaders.map(l => l.name)).toEqual(['pokemon-1', 'pokemon-2']);
      expect(component.selectedRound?.number).toBe(3);
    });

    it('sends other load failures to the error page', async () => {
      service.getTournament.and.returnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
      await create('abc');

      expect(router.navigate).toHaveBeenCalledWith(['/error'], { state: { returnUrl: '/tournaments/abc' } });
    });
  });
});
