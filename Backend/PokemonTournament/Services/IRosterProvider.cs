using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public interface IRosterProvider
    {
        /// <summary>
        /// Fetches the configured number of unique random Pokémon from PokeAPI.
        /// Returns null when any fetch fails so the caller can answer 503.
        /// </summary>
        Task<List<Pokemon>?> GetRandomRosterAsync();
    }
}
