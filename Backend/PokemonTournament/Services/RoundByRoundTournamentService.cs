using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public class RoundByRoundTournamentService : IRoundByRoundTournamentService
    {
        private readonly IRosterProvider _rosterProvider;
        private readonly ITournamentStore _store;
        private readonly IBattleService _battleService;
        private readonly TimeProvider _timeProvider;

        public RoundByRoundTournamentService(
            IRosterProvider rosterProvider,
            ITournamentStore store,
            IBattleService battleService,
            TimeProvider? timeProvider = null)
        {
            _rosterProvider = rosterProvider;
            _store = store;
            _battleService = battleService;
            _timeProvider = timeProvider ?? TimeProvider.System;
        }

        public async Task<Tournament?> StartAsync()
        {
            // PokeAPI is only called here; Rounds are processed from this snapshot.
            var roster = await _rosterProvider.GetRandomRosterAsync();
            if (roster == null)
            {
                return null;
            }

            var tournament = Tournament.Create(
                Guid.NewGuid(),
                _timeProvider.GetUtcNow(),
                roster.Select(Participant.From).ToList());

            _store.Add(tournament);
            return tournament;
        }

        public Tournament? Get(Guid id) => _store.Get(id);
    }
}
