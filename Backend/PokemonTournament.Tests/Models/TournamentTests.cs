using PokemonTournament.Enums;
using PokemonTournament.Models;

namespace PokemonTournament.Tests.Models;

public class TournamentTests
{
    private static readonly DateTimeOffset StartedAt = new(2026, 10, 8, 12, 0, 0, TimeSpan.Zero);

    internal static List<Participant> CreateParticipants(int count = 16) =>
        Enumerable.Range(1, count)
            .Select(i => new Participant(i, $"pokemon-{i:D2}", "normal", 100))
            .ToList();

    [Fact]
    public void Create_BuildsFullPendingScheduleWithSequentialBattleIds()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants());

        Assert.Equal(15, tournament.Rounds.Count);
        Assert.Equal(Enumerable.Range(1, 15), tournament.Rounds.Select(r => r.Number));
        Assert.All(tournament.Rounds, round =>
        {
            Assert.Equal(8, round.Battles.Count);
            Assert.All(round.Battles, battle =>
            {
                Assert.Equal(round.Number, battle.RoundNumber);
                Assert.False(battle.IsProcessed);
            });
        });
        Assert.Equal(
            Enumerable.Range(1, 120),
            tournament.Rounds.SelectMany(r => r.Battles).Select(b => b.Id));
    }

    [Fact]
    public void Create_StartsInProgressWithNoRoundsProcessed()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants());

        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.Equal(0, tournament.RoundsProcessed);
        Assert.Equal(1, tournament.NextRound?.Number);
        Assert.Equal(StartedAt, tournament.StartedAt);
    }

    [Fact]
    public void GetStandings_AtStart_EveryoneEvenAndSharingFirst()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants());

        var standings = tournament.GetStandings();

        Assert.Equal(16, standings.Count);
        Assert.All(standings, s =>
        {
            Assert.Equal(1, s.Rank);
            Assert.Equal(0, s.Wins + s.Losses + s.Ties);
        });
        Assert.Equal(standings.OrderBy(s => s.Participant.Name).Select(s => s.Participant.Name),
            standings.Select(s => s.Participant.Name));
    }

    [Fact]
    public void GetStandings_RanksByWinsThenTiesThenLossesThenName_WithSharedRanks()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants(4));
        // Round 1 pairs (0,3) and (1,2): seat 0 beats seat 3, seats 1 and 2 tie.
        var round1 = tournament.Rounds[0].Battles;
        RecordWinFor(round1.Single(b => Involves(b, 1)), 1);
        round1.Single(b => !Involves(b, 1)).Record(BattleResults.Ties, BattleOutcomeReason.EqualBaseExperience);

        var standings = tournament.GetStandings();

        Assert.Equal(new[] { 1, 2, 2, 4 }, standings.Select(s => s.Rank));
        Assert.Equal(1, standings[0].Participant.PokemonId);
        Assert.Equal(1, standings[0].Wins);
        Assert.Equal(new[] { "pokemon-02", "pokemon-03" }, standings.Skip(1).Take(2).Select(s => s.Participant.Name));
        Assert.Equal(1, standings[3].Losses);
    }

    [Fact]
    public void GetStandings_ThroughEarlierRound_IgnoresLaterRounds()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants(4));
        foreach (var battle in tournament.Rounds[0].Battles.Concat(tournament.Rounds[1].Battles))
        {
            battle.Record(BattleResults.FirstWins, BattleOutcomeReason.BaseExperience);
        }

        Assert.Equal(2, tournament.GetStandings(throughRound: 1).Sum(s => s.Wins));
        Assert.Equal(4, tournament.GetStandings().Sum(s => s.Wins));
    }

    [Fact]
    public void FindBattle_ReturnsBattleByIdOrNull()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants());

        Assert.Equal(2, tournament.FindBattle(9)?.RoundNumber);
        Assert.Null(tournament.FindBattle(0));
        Assert.Null(tournament.FindBattle(121));
    }

    [Fact]
    public void Record_WhenBattleAlreadyProcessed_Throws()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), StartedAt, CreateParticipants(2));
        var battle = tournament.Rounds[0].Battles[0];
        battle.Record(BattleResults.FirstWins, BattleOutcomeReason.TypeAdvantage);

        Assert.Throws<InvalidOperationException>(() =>
            battle.Record(BattleResults.SecondWins, BattleOutcomeReason.TypeAdvantage));
    }

    private static bool Involves(Battle battle, int pokemonId) =>
        battle.First.PokemonId == pokemonId || battle.Second.PokemonId == pokemonId;

    private static void RecordWinFor(Battle battle, int pokemonId) =>
        battle.Record(
            battle.First.PokemonId == pokemonId ? BattleResults.FirstWins : BattleResults.SecondWins,
            BattleOutcomeReason.TypeAdvantage);
}
