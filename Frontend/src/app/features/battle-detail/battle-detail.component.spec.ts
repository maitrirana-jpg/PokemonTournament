/// <reference types="jasmine" />

import { ComponentFixture, TestBed } from '@angular/core/testing';
import { HttpErrorResponse } from '@angular/common/http';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { BattleDetailComponent } from './battle-detail.component';
import { RoundByRoundService } from '../../services/round-by-round.service';
import { Battle } from '../../models/round-by-round.model';

describe('BattleDetailComponent', () => {
  let fixture: ComponentFixture<BattleDetailComponent>;
  let service: jasmine.SpyObj<RoundByRoundService>;

  const battle: Battle = {
    id: 12, roundNumber: 2, status: 'Processed',
    first: { id: 25, name: 'pikachu', type: 'electric', baseExperience: 112 },
    second: { id: 7, name: 'squirtle', type: 'water', baseExperience: 63 },
    outcome: 'FirstWins', winnerId: 25, reason: 'TypeAdvantage'
  };

  async function create(): Promise<void> {
    await TestBed.configureTestingModule({
      imports: [BattleDetailComponent],
      providers: [
        provideRouter([]),
        { provide: RoundByRoundService, useValue: service },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'abc', battleId: '12' }) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BattleDetailComponent);
    fixture.detectChanges();
  }

  const text = () => (fixture.nativeElement as HTMLElement).textContent ?? '';

  beforeEach(() => {
    service = jasmine.createSpyObj<RoundByRoundService>('RoundByRoundService', ['getBattle']);
  });

  it('shows who won, why, and both fighters', async () => {
    service.getBattle.and.returnValue(of(battle));
    await create();

    expect(service.getBattle).toHaveBeenCalledWith('abc', 12);
    expect(text()).toContain('Pikachu wins');
    expect(text()).toContain('Type advantage');
    expect(text()).toContain('squirtle');
    expect(text()).toContain('Back to round 2');
    expect(fixture.nativeElement.querySelectorAll('.fighter-card.winner').length).toBe(1);
  });

  it('shows a tie', async () => {
    service.getBattle.and.returnValue(of({ ...battle, outcome: 'Tie', winnerId: null, reason: 'EqualBaseExperience' }));
    await create();

    expect(text()).toContain('It\'s a tie');
    expect(text()).toContain('Equal base experience');
  });

  it('shows "no longer available" for a missing battle', async () => {
    service.getBattle.and.returnValue(throwError(() => new HttpErrorResponse({ status: 404 })));
    await create();

    expect(text()).toContain('This battle is no longer available');
  });
});
