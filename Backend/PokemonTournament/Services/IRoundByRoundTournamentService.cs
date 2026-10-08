using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public interface IRoundByRoundTournamentService
    {
        /// <summary>Starts a Tournament, or returns null when PokeAPI is unavailable.</summary>
        Task<Tournament?> StartAsync();

        Tournament? Get(Guid id);
    }
}
