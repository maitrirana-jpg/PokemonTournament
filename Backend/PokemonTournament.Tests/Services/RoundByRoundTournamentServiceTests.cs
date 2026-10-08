using Microsoft.Extensions.Options;
using Moq;
using PokemonTournament.Enums;
using PokemonTournament.Models;
using PokemonTournament.Services;

namespace PokemonTournament.Tests.Services;

public class RoundByRoundTournamentServiceTests
{
    private readonly Mock<IRosterProvider> _rosterProvider = new();
    private readonly InMemoryTournamentStore _store =
        new(Options.Create(new TournamentOptions { MaxStoredTournaments = 10 }));

    private RoundByRoundTournamentService CreateService() =>
        new(_rosterProvider.Object, _store, new BattleService());

    private static List<Pokemon> CreateRoster(int count = 16) =>
        Enumerable.Range(1, count)
            .Select(i => new Pokemon { Id = i, Name = $"pokemon-{i:D2}", Type = "water", BaseExperience = 50 + i })
            .ToList();

    [Fact]
    public async Task StartAsync_SnapshotsRosterAsParticipantsAndStoresTournament()
    {
        _rosterProvider.Setup(p => p.GetRandomRosterAsync()).ReturnsAsync(CreateRoster());

        var tournament = await CreateService().StartAsync();

        Assert.NotNull(tournament);
        Assert.Equal(16, tournament!.Participants.Count);
        Assert.Contains(tournament.Participants, p => p.PokemonId == 3 && p.BaseExperience == 53 && p.Type == "water");
        Assert.Equal(TournamentStatus.InProgress, tournament.Status);
        Assert.Same(tournament, _store.Get(tournament.Id));
    }

    [Fact]
    public async Task StartAsync_WhenRosterUnavailable_ReturnsNullAndStoresNothing()
    {
        _rosterProvider.Setup(p => p.GetRandomRosterAsync()).ReturnsAsync((List<Pokemon>?)null);

        var tournament = await CreateService().StartAsync();

        Assert.Null(tournament);
        Assert.Empty(_store.GetAll());
    }

    [Fact]
    public async Task Get_ReturnsStoredTournamentOrNull()
    {
        _rosterProvider.Setup(p => p.GetRandomRosterAsync()).ReturnsAsync(CreateRoster());
        var service = CreateService();
        var tournament = await service.StartAsync();

        Assert.Same(tournament, service.Get(tournament!.Id));
        Assert.Null(service.Get(Guid.NewGuid()));
    }
}

public class RoundByRoundTournamentServiceProcessTests
{
    private readonly Mock<IRosterProvider> _rosterProvider = new();
    private readonly Mock<IBattleService> _battleService = new();
    private readonly InMemoryTournamentStore _store =
        new(Options.Create(new TournamentOptions { MaxStoredTournaments = 10 }));
    private readonly RoundByRoundTournamentService _service;

    public RoundByRoundTournamentServiceProcessTests()
    {
        _rosterProvider.Setup(p => p.GetRandomRosterAsync()).ReturnsAsync(
            Enumerable.Range(1, 16)
                .Select(i => new Pokemon { Id = i, Name = $"pokemon-{i:D2}", Type = "water", BaseExperience = 100 })
                .ToList());
        _battleService
            .Setup(b => b.Decide(It.IsAny<Pokemon>(), It.IsAny<Pokemon>()))
            .Returns((BattleResults.FirstWins, BattleOutcomeReason.TypeAdvantage));
        _service = new RoundByRoundTournamentService(_rosterProvider.Object, _store, _battleService.Object);
    }

    [Fact]
    public async Task ProcessNextRound_DecidesOnlyTheNextRoundsBattles()
    {
        var tournament = (await _service.StartAsync())!;

        var result = _service.ProcessNextRound(tournament.Id, expectedRound: null);

        Assert.Equal(ProcessRoundStatus.Processed, result.Status);
        Assert.Equal(1, result.Round!.Number);
        Assert.All(result.Round.Battles, b =>
        {
            Assert.Equal(BattleResults.FirstWins, b.Result);
            Assert.Equal(BattleOutcomeReason.TypeAdvantage, b.Reason);
        });
        Assert.False(tournament.Rounds[1].IsProcessed);
        Assert.Equal(1, tournament.RoundsProcessed);
        _battleService.Verify(b => b.Decide(It.IsAny<Pokemon>(), It.IsAny<Pokemon>()), Times.Exactly(8));
    }

    [Fact]
    public async Task ProcessNextRound_PassesParticipantSnapshotsToBattleRules()
    {
        var tournament = (await _service.StartAsync())!;
        var battle = tournament.Rounds[0].Battles[0];

        _service.ProcessNextRound(tournament.Id, expectedRound: null);

        _battleService.Verify(b => b.Decide(
            It.Is<Pokemon>(p => p.Id == battle.First.PokemonId && p.Type == "water" && p.BaseExperience == 100),
            It.Is<Pokemon>(p => p.Id == battle.Second.PokemonId)));
    }

    [Fact]
    public async Task ProcessNextRound_After15Rounds_CompletesThenReportsAlreadyCompleted()
    {
        var tournament = (await _service.StartAsync())!;

        for (var round = 1; round <= 15; round++)
        {
            Assert.Equal(round, _service.ProcessNextRound(tournament.Id, round).Round!.Number);
        }

        Assert.Equal(TournamentStatus.Completed, tournament.Status);
        Assert.Equal(ProcessRoundStatus.AlreadyCompleted, _service.ProcessNextRound(tournament.Id, null).Status);
        _battleService.Verify(b => b.Decide(It.IsAny<Pokemon>(), It.IsAny<Pokemon>()), Times.Exactly(120));
    }

    [Fact]
    public async Task ProcessNextRound_WhenExpectedRoundIsNotNext_ReportsMismatchAndChangesNothing()
    {
        var tournament = (await _service.StartAsync())!;
        _service.ProcessNextRound(tournament.Id, 1);

        // A double-click resends expectedRound = 1 after round 1 is done.
        var result = _service.ProcessNextRound(tournament.Id, 1);

        Assert.Equal(ProcessRoundStatus.RoundMismatch, result.Status);
        Assert.Equal(2, result.NextRoundNumber);
        Assert.Equal(1, tournament.RoundsProcessed);
    }

    [Fact]
    public void ProcessNextRound_WhenTournamentMissing_ReportsNotFound()
    {
        Assert.Equal(ProcessRoundStatus.NotFound, _service.ProcessNextRound(Guid.NewGuid(), null).Status);
    }

    [Fact]
    public async Task ProcessNextRound_ConcurrentCalls_EachProcessADistinctRound()
    {
        var tournament = (await _service.StartAsync())!;

        var results = await Task.WhenAll(Enumerable.Range(0, 15)
            .Select(_ => Task.Run(() => _service.ProcessNextRound(tournament.Id, null))));

        Assert.Equal(Enumerable.Range(1, 15), results.Select(r => r.Round!.Number).OrderBy(n => n));
        Assert.Equal(TournamentStatus.Completed, tournament.Status);
    }
}
