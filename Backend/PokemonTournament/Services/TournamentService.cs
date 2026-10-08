using PokemonTournament.Enums;
using PokemonTournament.Infrastructure;
using PokemonTournament.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace PokemonTournament.Services
{
    public class TournamentService : ITournamentService
    {
        private readonly IBattleService _battleService;
        private readonly IRosterProvider _rosterProvider;
        private readonly ILogger<TournamentService> _logger;


        public TournamentService(
            IBattleService battleService,
            IPokeClient pokeClient,
            IOptions<TournamentOptions> options,
            ILogger<TournamentService>? logger = null,
            IAlertService? alertService = null)
        {
            _battleService = battleService;
            _logger = logger ?? NullLogger<TournamentService>.Instance;

            var tournamentOptions = options.Value;
            if (tournamentOptions.MinPokemonId < 1 ||
                tournamentOptions.MaxPokemonId < tournamentOptions.MinPokemonId ||
                tournamentOptions.DefaultParticipantCount < 2 ||
                tournamentOptions.DefaultParticipantCount > tournamentOptions.MaxPokemonId - tournamentOptions.MinPokemonId + 1 ||
                tournamentOptions.MaxConcurrentRequests < 1)
            {
                throw new ArgumentException("Tournament configuration is invalid.");
            }

            _rosterProvider = new RosterProvider(pokeClient, tournamentOptions, _logger, alertService);
        }

        public async Task<List<Pokemon>> GetTournamentResultsAsync(
            SortOptions sortOption,
            SortDirection sortDirection)
        {
            var stopwatch = Stopwatch.StartNew();

            var roaster = await _rosterProvider.GetRandomRosterAsync();

            // If something failed then return null so controller can decide
            if (roaster == null)
            {
                _logger.LogWarning(
                    "Tournament failed after {ElapsedMilliseconds} ms.",
                    stopwatch.ElapsedMilliseconds);
                return null;
            }

            // Generate tournament internally
            RunRoundRobin(roaster);

            // give result based on sorting
            var sortedResult = Sort(roaster, sortOption, sortDirection).ToList();
            _logger.LogInformation(
                "Tournament completed in {ElapsedMilliseconds} ms for {ParticipantCount} Pokemon.",
                stopwatch.ElapsedMilliseconds,
                sortedResult.Count);
            return sortedResult;
        }

        #region private methods

        private void RunRoundRobin(IList<Pokemon> roster)
        {
            for (var i = 0; i < roster.Count; i++)
            {
                for (var j = i + 1; j < roster.Count; j++)
                {
                    switch (_battleService.FightResult(roster[i], roster[j]))
                    {
                        case BattleResults.FirstWins:
                            roster[i].Wins++;
                            roster[j].Losses++;
                            break;
                        case BattleResults.SecondWins:
                            roster[j].Wins++;
                            roster[i].Losses++;
                            break;
                        default:
                            roster[i].Ties++;
                            roster[j].Ties++;
                            break;
                    }
                }
            }
        }

        private static IList<Pokemon> Sort(List<Pokemon> roster,SortOptions sortBy,SortDirection sortDirection)
        {
            List<Pokemon> sorted;

            if (sortBy == SortOptions.Wins)
            {
                sorted = roster.OrderBy(p => p.Wins).ToList();
            }
            else if (sortBy == SortOptions.Losses)
            {
                sorted = roster.OrderBy(p => p.Losses).ToList();
            }
            else if (sortBy == SortOptions.Ties)
            {
                sorted = roster.OrderBy(p => p.Ties).ToList();
            }
            else if (sortBy == SortOptions.Name)
            {
                sorted = roster.OrderBy(p => p.Name).ToList();
            }
            else if (sortBy ==  SortOptions.Id)
            {
                sorted = roster.OrderBy(p => p.Id).ToList();
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(sortBy));
            }

            if (sortDirection == SortDirection.Desc)
            {
                sorted.Reverse();
            }

            return sorted;
        }

        #endregion
    }
}
