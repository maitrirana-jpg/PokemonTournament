using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public interface IRoundByRoundTournamentService
    {
        /// <summary>Starts a Tournament, or returns null when PokeAPI is unavailable.</summary>
        Task<Tournament?> StartAsync();

        Tournament? Get(Guid id);

        /// <summary>Tournament History: stored Tournaments, newest first.</summary>
        IReadOnlyList<Tournament> GetHistory();

        /// <summary>
        /// Decides the Battles of the Tournament's next Round. When <paramref name="expectedRound"/>
        /// is given and is not the next Round (e.g. a repeated click), nothing is processed.
        /// </summary>
        ProcessRoundResult ProcessNextRound(Guid id, int? expectedRound);
    }
}
