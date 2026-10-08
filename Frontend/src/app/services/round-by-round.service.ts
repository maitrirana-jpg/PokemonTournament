import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../environment/environment';
import {
    Battle,
    ProcessRoundResponse,
    Round,
    Tournament,
    TournamentSummary
} from '../models/round-by-round.model';

@Injectable({
    providedIn: 'root'
})
export class RoundByRoundService {

    private apiUrl = environment.apiUrl + '/pokemon/tournament';

    constructor(private http: HttpClient) {}

    startTournament(): Observable<Tournament> {
        return this.http.post<Tournament>(this.apiUrl, null);
    }

    getTournament(id: string): Observable<Tournament> {
        return this.http.get<Tournament>(`${this.apiUrl}/${id}`);
    }

    /** expectedRound guards against processing twice on a repeated click (409). */
    processRound(id: string, expectedRound: number): Observable<ProcessRoundResponse> {
        const params = new HttpParams().set('expectedRound', expectedRound);

        return this.http.post<ProcessRoundResponse>(`${this.apiUrl}/${id}/rounds`, null, { params });
    }

    getRound(id: string, roundNumber: number): Observable<Round> {
        return this.http.get<Round>(`${this.apiUrl}/${id}/rounds/${roundNumber}`);
    }

    getBattle(id: string, battleId: number): Observable<Battle> {
        return this.http.get<Battle>(`${this.apiUrl}/${id}/battles/${battleId}`);
    }

    getHistory(): Observable<TournamentSummary[]> {
        return this.http.get<TournamentSummary[]>(`${this.apiUrl}/history`);
    }
}
