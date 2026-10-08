namespace PokemonTournament.Services
{
    /// <summary>
    /// Builds a full round-robin Schedule with the circle method:
    /// participant 0 stays fixed while the others rotate one seat per round,
    /// so every pair meets exactly once over (n - 1) rounds of n / 2 battles.
    /// Pairs are participant indexes; callers shuffle the roster for variety.
    /// </summary>
    public static class ScheduleGenerator
    {
        public static IReadOnlyList<IReadOnlyList<(int First, int Second)>> Generate(int participantCount)
        {
            if (participantCount < 2 || participantCount % 2 != 0)
            {
                throw new ArgumentException(
                    "A round-robin schedule needs an even number of at least two participants.",
                    nameof(participantCount));
            }

            var seats = Enumerable.Range(0, participantCount).ToList();
            var rounds = new List<IReadOnlyList<(int First, int Second)>>(participantCount - 1);

            for (var round = 0; round < participantCount - 1; round++)
            {
                var pairs = new List<(int First, int Second)>(participantCount / 2);
                for (var seat = 0; seat < participantCount / 2; seat++)
                {
                    pairs.Add((seats[seat], seats[participantCount - 1 - seat]));
                }
                rounds.Add(pairs);

                // Rotate everyone except the fixed seat 0 one place clockwise.
                var last = seats[participantCount - 1];
                seats.RemoveAt(participantCount - 1);
                seats.Insert(1, last);
            }

            return rounds;
        }
    }
}
