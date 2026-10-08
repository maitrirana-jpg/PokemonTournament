import { Battle, BattleOutcomeReason, Round } from '../models/round-by-round.model';

export type RoundResult = 'W' | 'L' | 'T';

const reasonLabels: Record<BattleOutcomeReason, string> = {
    TypeAdvantage: 'Type advantage',
    BaseExperience: 'Higher base experience',
    EqualBaseExperience: 'Equal base experience'
};

export function getReasonLabel(reason: BattleOutcomeReason | null): string {
    return reason ? reasonLabels[reason] : 'Not fought yet';
}

/** A Participant's result in one processed Battle, or null if pending or not in it. */
export function getResultFor(battle: Battle, participantId: number): RoundResult | null {
    if (battle.status !== 'Processed' ||
        (battle.first.id !== participantId && battle.second.id !== participantId)) {
        return null;
    }

    if (battle.winnerId === null) {
        return 'T';
    }

    return battle.winnerId === participantId ? 'W' : 'L';
}

/** Each Participant's W/L/T in a processed Round, keyed by Pokémon id. */
export function getRoundResults(round: Round): Map<number, RoundResult> {
    const results = new Map<number, RoundResult>();

    for (const battle of round.battles) {
        for (const participant of [battle.first, battle.second]) {
            const result = getResultFor(battle, participant.id);
            if (result) {
                results.set(participant.id, result);
            }
        }
    }

    return results;
}
