# Pokémon Tournament

Pits 16 randomly chosen Pokémon against each other in a round-robin and ranks them by results.

## Language

### Tournaments

**Tournament**:
A persisted round-robin of 16 Participants that the user advances one Round at a time and can review afterwards.
_Avoid_: Run, game, session

**In Progress / Completed**:
A Tournament is In Progress until its 15th Round is processed, then Completed. An In Progress Tournament can be resumed from Tournament History.

**Quick Tournament**:
The original one-shot calculation that resolves all Battles at once and returns final Standings without keeping anything. It is not a Tournament and never appears in Tournament History.
_Avoid_: Statistics run, classic tournament

**Tournament History**:
The list of Tournaments started so far (most recent first), bounded to a configured number.

**Participant**:
A snapshot of one Pokémon (name, primary type, base experience) taken when its Tournament starts; later changes in PokeAPI never affect it.
_Avoid_: Player, competitor, entrant

### Play

**Schedule**:
The complete, fixed pairing of all Participants into 15 Rounds, decided when the Tournament starts, such that every pair meets exactly once.
_Avoid_: Fixture list, bracket

**Round**:
One slot of the Schedule in which every Participant fights exactly once, i.e. 8 Battles. A Round is either pending or processed.
_Avoid_: Stage, matchday

**Battle**:
One scheduled pairing of two Participants. Before its Round is processed it has no outcome; once processed it has an Outcome and an Outcome Reason.
_Avoid_: Match, fight, duel

**Outcome**:
The result of a Battle: first Participant wins, second Participant wins, or tie.

**Outcome Reason**:
Why a Battle's Outcome was reached: type advantage, higher base experience, or equal base experience (tie).

**Standings**:
Each Participant's wins, losses and ties over processed Rounds, ranked by wins (desc), ties (desc), losses (asc), then name.
_Avoid_: Leaderboard, statistics, table

**Leaders**:
All Participants sharing the best wins/ties/losses record in the Standings. A Completed Tournament has Leaders, not a single winner; the name ordering is for display only.
_Avoid_: Champion, winner (of a Tournament)
