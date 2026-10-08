using PokemonTournament.Infrastructure;
using PokemonTournament.Models;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

namespace PokemonTournament.Services
{
    public class RosterProvider : IRosterProvider
    {
        private readonly IPokeClient _pokeClient;
        private readonly TournamentOptions _options;
        private readonly ILogger _logger;
        private readonly IAlertService _alertService;

        public RosterProvider(
            IPokeClient pokeClient,
            IOptions<TournamentOptions> options,
            ILogger<RosterProvider>? logger = null,
            IAlertService? alertService = null)
            : this(pokeClient, options.Value, logger, alertService)
        {
        }

        internal RosterProvider(
            IPokeClient pokeClient,
            TournamentOptions options,
            ILogger? logger,
            IAlertService? alertService)
        {
            _pokeClient = pokeClient;
            _options = options;
            _logger = logger ?? NullLogger.Instance;
            _alertService = alertService ?? new LoggingAlertService(
                NullLogger<LoggingAlertService>.Instance);
        }

        public async Task<List<Pokemon>?> GetRandomRosterAsync()
        {
            var randomIds = SelectRandomIds();

            // Get the json from API to fetch pokemons
            var responses = new List<PokemonAPIResponse>(randomIds.Count);

            bool stopProcessing = false;

            await Parallel.ForEachAsync(
                randomIds,
                new ParallelOptions { MaxDegreeOfParallelism = _options.MaxConcurrentRequests },
                async (id, cancellationToken) =>
                {
                    if (stopProcessing)
                        return;

                    try
                    {
                        var response = await _pokeClient.GetPokemonAsync(id);

                        if (response != null && response.Types != null)
                        {
                            lock (responses)
                            {
                                responses.Add(response);
                            }
                        }
                        else
                        {
                            stopProcessing = true;
                            _alertService.Raise(
                                "PokeAPI returned an incomplete response.",
                                new InvalidOperationException($"Pokemon {id} was missing data."));
                        }
                    }
                    catch (Exception exception)
                    {
                        stopProcessing = true;
                        _alertService.Raise($"PokeAPI request failed for Pokemon {id}.", exception);
                    }
                });

            // If something failed then return null so caller can decide
            if (stopProcessing || responses.Count != randomIds.Count)
            {
                _logger.LogWarning(
                    "Roster fetch failed. Requested {RequestedCount} Pokemon and received {ResponseCount}.",
                    randomIds.Count,
                    responses.Count);
                return null;
            }

            return responses.Select(response => ConvertToPokemon(response)).ToList();
        }

        #region private methods

        private List<int> SelectRandomIds()
        {
            var ids = Enumerable.Range( _options.MinPokemonId,_options.MaxPokemonId - _options.MinPokemonId + 1).ToArray();

            for (var index = 0; index < _options.DefaultParticipantCount; index++)
            {
                var swapIndex = Random.Shared.Next(index, ids.Length);
                (ids[index], ids[swapIndex]) = (ids[swapIndex], ids[index]);
            }

            return ids.Take(_options.DefaultParticipantCount).ToList();
        }

        private static Pokemon ConvertToPokemon(PokemonAPIResponse response)
        {
            string primaryType = "";
            foreach (var t in response.Types)
            {
                if (t.Slot == 1)
                {
                    primaryType = t.PokemonAPIType.Name;
                    break;
                }
            }
            return new Pokemon
            {
                Id = response.Id,
                Name = response.Name,
                Type = primaryType,
                BaseExperience = response.BaseExperience,
            };
        }

        #endregion
    }
}
