namespace PokemonTournament.Models
{
    // Enum-like values are exposed as strings so the API reads well and the
    // global JSON settings used by the original endpoint stay untouched.

    public sealed record ParticipantDto(int Id, string Name, string Type, int BaseExperience)
    {
        public static ParticipantDto From(Participant participant) =>
            new(participant.PokemonId, participant.Name, participant.Type, participant.BaseExperience);
    }

    public sealed record StandingDto(int Rank, int Id, string Name, string Type, int Wins, int Losses, int Ties)
    {
        public static StandingDto From(Standing standing) =>
            new(standing.Rank,
                standing.Participant.PokemonId,
                standing.Participant.Name,
                standing.Participant.Type,
                standing.Wins,
                standing.Losses,
                standing.Ties);
    }

    /// <summary>
    /// Status is "Pending" or "Processed". Outcome ("FirstWins", "SecondWins", "Tie"),
    /// WinnerId and Reason are null while the Battle is pending.
    /// </summary>
    public sealed record BattleDto(
        int Id,
        int RoundNumber,
        string Status,
        ParticipantDto First,
        ParticipantDto Second,
        string? Outcome,
        int? WinnerId,
        string? Reason)
    {
        public static BattleDto From(Battle battle) =>
            new(battle.Id,
                battle.RoundNumber,
                battle.IsProcessed ? "Processed" : "Pending",
                ParticipantDto.From(battle.First),
                ParticipantDto.From(battle.Second),
                battle.Result switch
                {
                    null => null,
                    Enums.BattleResults.Ties => "Tie",
                    var result => result.ToString()
                },
                battle.Winner?.PokemonId,
                battle.Reason?.ToString());
    }

    /// <summary>
    /// Standings are as of the end of this Round, and only present once it is processed.
    /// </summary>
    public sealed record RoundDto(
        int Number,
        string Status,
        IReadOnlyList<BattleDto> Battles,
        IReadOnlyList<StandingDto>? Standings)
    {
        public static RoundDto From(Tournament tournament, Round round) =>
            new(round.Number,
                round.IsProcessed ? "Processed" : "Pending",
                round.Battles.Select(BattleDto.From).ToList(),
                round.IsProcessed
                    ? tournament.GetStandings(round.Number).Select(StandingDto.From).ToList()
                    : null);
    }

    public sealed record TournamentDto(
        Guid Id,
        DateTimeOffset StartedAt,
        string Status,
        int RoundsProcessed,
        int TotalRounds,
        IReadOnlyList<ParticipantDto> Participants,
        IReadOnlyList<RoundDto> Rounds,
        IReadOnlyList<StandingDto> Standings)
    {
        public static TournamentDto From(Tournament tournament) =>
            new(tournament.Id,
                tournament.StartedAt,
                tournament.Status.ToString(),
                tournament.RoundsProcessed,
                tournament.Rounds.Count,
                tournament.Participants.Select(ParticipantDto.From).ToList(),
                tournament.Rounds.Select(round => RoundDto.From(tournament, round)).ToList(),
                tournament.GetStandings().Select(StandingDto.From).ToList());
    }
}
