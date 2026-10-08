using PokemonTournament.Enums;
using PokemonTournament.Services;

namespace PokemonTournament.Models
{
    /// <summary>
    /// Snapshot of a Pokémon taken when its Tournament starts.
    /// </summary>
    public sealed record Participant(int PokemonId, string Name, string Type, int BaseExperience)
    {
        public static Participant From(Pokemon pokemon) =>
            new(pokemon.Id, pokemon.Name, pokemon.Type, pokemon.BaseExperience);

        public Pokemon ToPokemon() =>
            new() { Id = PokemonId, Name = Name, Type = Type, BaseExperience = BaseExperience };
    }

    public sealed class Battle
    {
        public Battle(int id, int roundNumber, Participant first, Participant second)
        {
            Id = id;
            RoundNumber = roundNumber;
            First = first;
            Second = second;
        }

        public int Id { get; }
        public int RoundNumber { get; }
        public Participant First { get; }
        public Participant Second { get; }
        public BattleResults? Result { get; private set; }
        public BattleOutcomeReason? Reason { get; private set; }
        public bool IsProcessed => Result.HasValue;

        public Participant? Winner => Result switch
        {
            BattleResults.FirstWins => First,
            BattleResults.SecondWins => Second,
            _ => null
        };

        public void Record(BattleResults result, BattleOutcomeReason reason)
        {
            if (IsProcessed)
            {
                throw new InvalidOperationException($"Battle {Id} has already been processed.");
            }

            Result = result;
            Reason = reason;
        }
    }

    public sealed class Round
    {
        public Round(int number, IReadOnlyList<Battle> battles)
        {
            Number = number;
            Battles = battles;
        }

        public int Number { get; }
        public IReadOnlyList<Battle> Battles { get; }
        public bool IsProcessed => Battles.All(b => b.IsProcessed);
    }

    public enum TournamentStatus
    {
        InProgress,
        Completed
    }

    public sealed record Standing(int Rank, Participant Participant, int Wins, int Losses, int Ties);

    public sealed class Tournament
    {
        private Tournament(
            Guid id,
            DateTimeOffset startedAt,
            IReadOnlyList<Participant> participants,
            IReadOnlyList<Round> rounds)
        {
            Id = id;
            StartedAt = startedAt;
            Participants = participants;
            Rounds = rounds;
        }

        public Guid Id { get; }
        public DateTimeOffset StartedAt { get; }
        public IReadOnlyList<Participant> Participants { get; }
        public IReadOnlyList<Round> Rounds { get; }

        /// <summary>Guards processing so concurrent requests advance one Round at a time.</summary>
        internal object SyncRoot { get; } = new();

        public int RoundsProcessed => Rounds.Count(r => r.IsProcessed);

        public TournamentStatus Status =>
            RoundsProcessed == Rounds.Count ? TournamentStatus.Completed : TournamentStatus.InProgress;

        public Round? NextRound => Rounds.FirstOrDefault(r => !r.IsProcessed);

        /// <summary>
        /// Creates a Tournament with its full Schedule fixed up front. Participants keep
        /// the order given (the roster is already random); Battle ids run 1..N in Schedule order.
        /// </summary>
        public static Tournament Create(Guid id, DateTimeOffset startedAt, IReadOnlyList<Participant> participants)
        {
            var schedule = ScheduleGenerator.Generate(participants.Count);
            var battleId = 1;

            var rounds = schedule
                .Select((pairs, index) => new Round(
                    index + 1,
                    pairs.Select(pair => new Battle(
                            battleId++,
                            index + 1,
                            participants[pair.First],
                            participants[pair.Second]))
                        .ToList()))
                .ToList();

            return new Tournament(id, startedAt, participants.ToList(), rounds);
        }

        public Round? FindRound(int number) => Rounds.FirstOrDefault(r => r.Number == number);

        public Battle? FindBattle(int battleId) =>
            Rounds.SelectMany(r => r.Battles).FirstOrDefault(b => b.Id == battleId);

        /// <summary>
        /// Standings over processed Battles up to and including <paramref name="throughRound"/>
        /// (all Rounds when null), ranked by wins desc, ties desc, losses asc, then name.
        /// Equal records share a rank.
        /// </summary>
        /// <summary>
        /// Participants sharing the best record; empty before any Round is processed.
        /// </summary>
        public IReadOnlyList<Standing> GetLeaders() =>
            RoundsProcessed == 0
                ? Array.Empty<Standing>()
                : GetStandings().Where(s => s.Rank == 1).ToList();

        public IReadOnlyList<Standing> GetStandings(int? throughRound = null)
        {
            var records = Participants.ToDictionary(p => p, _ => (Wins: 0, Losses: 0, Ties: 0));

            var battles = Rounds
                .Where(r => throughRound is null || r.Number <= throughRound)
                .SelectMany(r => r.Battles)
                .Where(b => b.IsProcessed);

            foreach (var battle in battles)
            {
                var first = records[battle.First];
                var second = records[battle.Second];

                switch (battle.Result)
                {
                    case BattleResults.FirstWins:
                        first.Wins++;
                        second.Losses++;
                        break;
                    case BattleResults.SecondWins:
                        second.Wins++;
                        first.Losses++;
                        break;
                    default:
                        first.Ties++;
                        second.Ties++;
                        break;
                }

                records[battle.First] = first;
                records[battle.Second] = second;
            }

            var ordered = records
                .OrderByDescending(r => r.Value.Wins)
                .ThenByDescending(r => r.Value.Ties)
                .ThenBy(r => r.Value.Losses)
                .ThenBy(r => r.Key.Name, StringComparer.Ordinal)
                .ToList();

            var standings = new List<Standing>(ordered.Count);
            for (var index = 0; index < ordered.Count; index++)
            {
                var (participant, record) = (ordered[index].Key, ordered[index].Value);
                var rank = index > 0 && ordered[index - 1].Value == record
                    ? standings[index - 1].Rank
                    : index + 1;

                standings.Add(new Standing(rank, participant, record.Wins, record.Losses, record.Ties));
            }

            return standings;
        }
    }
}
