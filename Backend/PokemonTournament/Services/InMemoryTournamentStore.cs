using Microsoft.Extensions.Options;
using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    /// <summary>
    /// Process-wide store holding the most recent Tournaments; the oldest is evicted
    /// once the configured capacity is reached (see docs/adr/0001).
    /// </summary>
    public class InMemoryTournamentStore : ITournamentStore
    {
        private readonly int _capacity;
        private readonly LinkedList<Tournament> _newestFirst = new();
        private readonly Dictionary<Guid, LinkedListNode<Tournament>> _byId = new();
        private readonly object _lock = new();

        public InMemoryTournamentStore(IOptions<TournamentOptions> options)
        {
            _capacity = options.Value.MaxStoredTournaments;

            if (_capacity < 1)
            {
                throw new ArgumentException("MaxStoredTournaments must be at least 1.");
            }
        }

        public void Add(Tournament tournament)
        {
            lock (_lock)
            {
                _byId[tournament.Id] = _newestFirst.AddFirst(tournament);

                while (_newestFirst.Count > _capacity)
                {
                    var oldest = _newestFirst.Last!;
                    _newestFirst.RemoveLast();
                    _byId.Remove(oldest.Value.Id);
                }
            }
        }

        public Tournament? Get(Guid id)
        {
            lock (_lock)
            {
                return _byId.TryGetValue(id, out var node) ? node.Value : null;
            }
        }

        public IReadOnlyList<Tournament> GetAll()
        {
            lock (_lock)
            {
                return _newestFirst.ToList();
            }
        }
    }
}
