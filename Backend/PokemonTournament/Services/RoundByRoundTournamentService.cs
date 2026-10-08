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

        public ProcessRoundResult ProcessNextRound(Guid id, int? expectedRound)
        {
            var tournament = _store.Get(id);
            if (tournament == null)
            {
                return ProcessRoundResult.NotFound();
            }

            lock (tournament.SyncRoot)
            {
                var round = tournament.NextRound;
                if (round == null)
                {
                    return ProcessRoundResult.AlreadyCompleted();
                }

                if (expectedRound.HasValue && expectedRound.Value != round.Number)
                {
                    return ProcessRoundResult.RoundMismatch(round.Number);
                }

                foreach (var battle in round.Battles)
                {
                    var (result, reason) = _battleService.Decide(battle.First.ToPokemon(), battle.Second.ToPokemon());
                    battle.Record(result, reason);
                }

                return ProcessRoundResult.Processed(tournament, round);
            }
        }
    }
}
