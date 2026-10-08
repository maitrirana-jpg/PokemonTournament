using PokemonTournament.Services;

namespace PokemonTournament.Tests.Services;

public class ScheduleGeneratorTests
{
    [Fact]
    public void Generate_For16Participants_Returns15RoundsOf8Battles()
    {
        var schedule = ScheduleGenerator.Generate(16);

        Assert.Equal(15, schedule.Count);
        Assert.All(schedule, round => Assert.Equal(8, round.Count));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    public void Generate_EveryParticipantFightsExactlyOncePerRound(int participantCount)
    {
        var schedule = ScheduleGenerator.Generate(participantCount);

        foreach (var round in schedule)
        {
            var fighters = round.SelectMany(pair => new[] { pair.First, pair.Second }).OrderBy(i => i);
            Assert.Equal(Enumerable.Range(0, participantCount), fighters);
        }
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(16)]
    public void Generate_EveryPairMeetsExactlyOnce(int participantCount)
    {
        var schedule = ScheduleGenerator.Generate(participantCount);

        var pairs = schedule
            .SelectMany(round => round)
            .Select(pair => (Math.Min(pair.First, pair.Second), Math.Max(pair.First, pair.Second)))
            .ToList();

        Assert.Equal(participantCount * (participantCount - 1) / 2, pairs.Count);
        Assert.Equal(pairs.Count, pairs.Distinct().Count());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(3)]
    public void Generate_WhenParticipantCountIsNotEvenAndAtLeastTwo_Throws(int participantCount)
    {
        Assert.Throws<ArgumentException>(() => ScheduleGenerator.Generate(participantCount));
    }
}
