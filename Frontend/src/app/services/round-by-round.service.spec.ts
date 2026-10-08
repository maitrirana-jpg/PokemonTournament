/// <reference types="jasmine" />

import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { RoundByRoundService } from './round-by-round.service';
import { environment } from '../../environment/environment';

describe('RoundByRoundService', () => {
  const base = `${environment.apiUrl}/pokemon/tournament`;
  const id = '664421f0-0510-474a-8f52-f70ce435bd97';
  let service: RoundByRoundService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(RoundByRoundService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts a tournament with a POST', () => {
    service.startTournament().subscribe();

    const request = http.expectOne(base);
    expect(request.request.method).toBe('POST');
    request.flush({});
  });

  it('gets a tournament by id', () => {
    service.getTournament(id).subscribe();

    expect(http.expectOne(`${base}/${id}`).request.method).toBe('GET');
  });

  it('processes the next round, passing the expected round', () => {
    service.processRound(id, 4).subscribe();

    const request = http.expectOne(r => r.url === `${base}/${id}/rounds`);
    expect(request.request.method).toBe('POST');
    expect(request.request.params.get('expectedRound')).toBe('4');
  });

  it('gets one round for review', () => {
    service.getRound(id, 3).subscribe();

    expect(http.expectOne(`${base}/${id}/rounds/3`).request.method).toBe('GET');
  });

  it('gets one battle for review', () => {
    service.getBattle(id, 42).subscribe();

    expect(http.expectOne(`${base}/${id}/battles/42`).request.method).toBe('GET');
  });

  it('gets tournament history', () => {
    service.getHistory().subscribe();

    expect(http.expectOne(`${base}/history`).request.method).toBe('GET');
  });
});
