// Mirrors the round-by-round Tournament API (see CONTEXT.md for the vocabulary).

export type TournamentStatus = 'InProgress' | 'Completed';
export type RoundStatus = 'Pending' | 'Processed';
export type BattleOutcome = 'FirstWins' | 'SecondWins' | 'Tie';
export type BattleOutcomeReason = 'TypeAdvantage' | 'BaseExperience' | 'EqualBaseExperience';

export interface Participant {
    id: number;
    name: string;
    type: string;
    baseExperience: number;
}

export interface Standing {
    rank: number;
    id: number;
    name: string;
    type: string;
    wins: number;
    losses: number;
    ties: number;
}

export interface Battle {
    id: number;
    roundNumber: number;
    status: RoundStatus;
    first: Participant;
    second: Participant;
    outcome: BattleOutcome | null;
    winnerId: number | null;
    reason: BattleOutcomeReason | null;
}

export interface Round {
    number: number;
    status: RoundStatus;
    battles: Battle[];
    /** Standings as of the end of this Round; null while it is pending. */
    standings: Standing[] | null;
}

export interface Tournament {
    id: string;
    startedAt: string;
    status: TournamentStatus;
    roundsProcessed: number;
    totalRounds: number;
    participants: Participant[];
    rounds: Round[];
    standings: Standing[];
}

export interface ProcessRoundResponse {
    round: Round;
    standings: Standing[];
    status: TournamentStatus;
    roundsProcessed: number;
    totalRounds: number;
}

export interface TournamentSummary {
    id: string;
    startedAt: string;
    status: TournamentStatus;
    roundsProcessed: number;
    totalRounds: number;
    /** Participants sharing the best record; empty until a Round is processed. */
    leaders: Standing[];
}
