using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public interface ITournamentStore
    {
        void Add(Tournament tournament);

        Tournament? Get(Guid id);

        /// <summary>Stored Tournaments, newest first.</summary>
        IReadOnlyList<Tournament> GetAll();
    }
}
