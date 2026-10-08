using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public enum ProcessRoundStatus
    {
        Processed,
        NotFound,
        AlreadyCompleted,
        RoundMismatch
    }

    /// <summary>
    /// Outcome of asking a Tournament to process its next Round. Round and Tournament
    /// are set only when Processed; NextRoundNumber only on RoundMismatch.
    /// </summary>
    public sealed record ProcessRoundResult(
        ProcessRoundStatus Status,
        Tournament? Tournament = null,
        Round? Round = null,
        int? NextRoundNumber = null)
    {
        public static ProcessRoundResult Processed(Tournament tournament, Round round) =>
            new(ProcessRoundStatus.Processed, tournament, round);

        public static ProcessRoundResult NotFound() => new(ProcessRoundStatus.NotFound);

        public static ProcessRoundResult AlreadyCompleted() => new(ProcessRoundStatus.AlreadyCompleted);

        public static ProcessRoundResult RoundMismatch(int nextRoundNumber) =>
            new(ProcessRoundStatus.RoundMismatch, NextRoundNumber: nextRoundNumber);
    }
}
