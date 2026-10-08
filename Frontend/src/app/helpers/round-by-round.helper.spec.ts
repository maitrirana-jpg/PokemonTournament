/// <reference types="jasmine" />

import { getReasonLabel, getResultFor, getRoundResults } from './round-by-round.helper';
import { Battle, Participant, Round } from '../models/round-by-round.model';

describe('round-by-round helper', () => {
  const pikachu: Participant = { id: 25, name: 'pikachu', type: 'electric', baseExperience: 112 };
  const squirtle: Participant = { id: 7, name: 'squirtle', type: 'water', baseExperience: 63 };

  const battle = (overrides: Partial<Battle>): Battle => ({
    id: 1,
    roundNumber: 1,
    status: 'Processed',
    first: pikachu,
    second: squirtle,
    outcome: 'FirstWins',
    winnerId: 25,
    reason: 'TypeAdvantage',
    ...overrides
  });

  it('labels outcome reasons for people', () => {
    expect(getReasonLabel('TypeAdvantage')).toBe('Type advantage');
    expect(getReasonLabel('BaseExperience')).toBe('Higher base experience');
    expect(getReasonLabel('EqualBaseExperience')).toBe('Equal base experience');
    expect(getReasonLabel(null)).toBe('Not fought yet');
  });

  it('gives each side its result in a processed battle', () => {
    expect(getResultFor(battle({}), 25)).toBe('W');
    expect(getResultFor(battle({}), 7)).toBe('L');
    expect(getResultFor(battle({ outcome: 'Tie', winnerId: null }), 7)).toBe('T');
  });

  it('gives no result for a pending battle or a non-participant', () => {
    expect(getResultFor(battle({ status: 'Pending', outcome: null, winnerId: null }), 25)).toBeNull();
    expect(getResultFor(battle({}), 1)).toBeNull();
  });

  it('collects every participant result in a round', () => {
    const round: Round = { number: 1, status: 'Processed', battles: [battle({})], standings: [] };

    const results = getRoundResults(round);

    expect(results.get(25)).toBe('W');
    expect(results.get(7)).toBe('L');
    expect(results.size).toBe(2);
  });
});
