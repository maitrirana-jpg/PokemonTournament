using Microsoft.AspNetCore.Mvc;
using Moq;
using PokemonTournament.Controllers;
using PokemonTournament.Models;
using PokemonTournament.Services;
using PokemonTournament.Tests.Models;

namespace PokemonTournament.Tests.Controllers;

public class TournamentControllerTests
{
    private readonly Mock<IRoundByRoundTournamentService> _service = new();
    private readonly TournamentController _controller;

    public TournamentControllerTests()
    {
        _controller = new TournamentController(_service.Object);
    }

    private static Tournament CreateTournament() =>
        Tournament.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, TournamentTests.CreateParticipants());

    [Fact]
    public async Task StartTournament_ReturnsCreatedWithIdAndParticipants()
    {
        var tournament = CreateTournament();
        _service.Setup(s => s.StartAsync()).ReturnsAsync(tournament);

        var result = await _controller.StartTournament();

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(TournamentController.GetTournament), created.ActionName);
        var dto = Assert.IsType<TournamentDto>(created.Value);
        Assert.Equal(tournament.Id, dto.Id);
        Assert.Equal(16, dto.Participants.Count);
        Assert.Equal("InProgress", dto.Status);
        Assert.All(dto.Standings, s => Assert.Equal(0, s.Wins + s.Losses + s.Ties));
    }

    [Fact]
    public async Task StartTournament_WhenPokeApiUnavailable_Returns503()
    {
        _service.Setup(s => s.StartAsync()).ReturnsAsync((Tournament?)null);

        var result = await _controller.StartTournament();

        var status = Assert.IsType<ObjectResult>(result);
        Assert.Equal(503, status.StatusCode);
    }

    [Fact]
    public void GetTournament_WhenFound_ReturnsFullTournament()
    {
        var tournament = CreateTournament();
        _service.Setup(s => s.Get(tournament.Id)).Returns(tournament);

        var result = _controller.GetTournament(tournament.Id);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<TournamentDto>(ok.Value);
        Assert.Equal(15, dto.TotalRounds);
        Assert.Equal(15, dto.Rounds.Count);
        Assert.All(dto.Rounds, r => Assert.Equal("Pending", r.Status));
        Assert.Equal("Pending", dto.Rounds[0].Battles[0].Status);
        Assert.Null(dto.Rounds[0].Battles[0].Outcome);
    }

    [Fact]
    public void GetTournament_WhenMissing_Returns404()
    {
        var result = _controller.GetTournament(Guid.NewGuid());

        Assert.IsType<NotFoundObjectResult>(result);
    }
}

public class TournamentControllerProcessRoundTests
{
    private readonly Mock<IRoundByRoundTournamentService> _service = new();
    private readonly TournamentController _controller;

    public TournamentControllerProcessRoundTests()
    {
        _controller = new TournamentController(_service.Object);
    }

    [Fact]
    public void ProcessRound_WhenProcessed_ReturnsRoundResultsAndStandings()
    {
        var tournament = Tournament.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, TournamentTests.CreateParticipants());
        var round = tournament.Rounds[0];
        foreach (var battle in round.Battles)
        {
            battle.Record(PokemonTournament.Enums.BattleResults.FirstWins, PokemonTournament.Enums.BattleOutcomeReason.TypeAdvantage);
        }
        _service.Setup(s => s.ProcessNextRound(tournament.Id, 1))
            .Returns(ProcessRoundResult.Processed(tournament, round));

        var result = _controller.ProcessRound(tournament.Id, 1);

        var ok = Assert.IsType<OkObjectResult>(result);
        var dto = Assert.IsType<ProcessRoundResponseDto>(ok.Value);
        Assert.Equal(1, dto.Round.Number);
        Assert.Equal(8, dto.Round.Battles.Count);
        Assert.All(dto.Round.Battles, b =>
        {
            Assert.Equal("Processed", b.Status);
            Assert.Equal("FirstWins", b.Outcome);
            Assert.Equal(b.First.Id, b.WinnerId);
            Assert.Equal("TypeAdvantage", b.Reason);
        });
        Assert.Equal(8, dto.Standings.Sum(s => s.Wins));
        Assert.Equal(1, dto.RoundsProcessed);
        Assert.Equal("InProgress", dto.Status);
    }

    [Fact]
    public void ProcessRound_WhenMissing_Returns404()
    {
        _service.Setup(s => s.ProcessNextRound(It.IsAny<Guid>(), It.IsAny<int?>()))
            .Returns(ProcessRoundResult.NotFound());

        Assert.IsType<NotFoundObjectResult>(_controller.ProcessRound(Guid.NewGuid(), null));
    }

    [Fact]
    public void ProcessRound_WhenCompleted_Returns409()
    {
        _service.Setup(s => s.ProcessNextRound(It.IsAny<Guid>(), It.IsAny<int?>()))
            .Returns(ProcessRoundResult.AlreadyCompleted());

        Assert.IsType<ConflictObjectResult>(_controller.ProcessRound(Guid.NewGuid(), null));
    }

    [Fact]
    public void ProcessRound_WhenExpectedRoundMismatch_Returns409()
    {
        _service.Setup(s => s.ProcessNextRound(It.IsAny<Guid>(), It.IsAny<int?>()))
            .Returns(ProcessRoundResult.RoundMismatch(3));

        Assert.IsType<ConflictObjectResult>(_controller.ProcessRound(Guid.NewGuid(), 2));
    }
}

public class TournamentControllerReviewTests
{
    private readonly Mock<IRoundByRoundTournamentService> _service = new();
    private readonly TournamentController _controller;
    private readonly Tournament _tournament =
        Tournament.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, TournamentTests.CreateParticipants());

    public TournamentControllerReviewTests()
    {
        _controller = new TournamentController(_service.Object);
        _service.Setup(s => s.Get(_tournament.Id)).Returns(_tournament);

        // Round 1 processed: the first Participant of every Battle wins.
        foreach (var battle in _tournament.Rounds[0].Battles)
        {
            battle.Record(PokemonTournament.Enums.BattleResults.FirstWins, PokemonTournament.Enums.BattleOutcomeReason.BaseExperience);
        }
    }

    [Fact]
    public void GetRound_WhenProcessed_ReturnsBattlesAndStandingsAsOfThatRound()
    {
        var ok = Assert.IsType<OkObjectResult>(_controller.GetRound(_tournament.Id, 1));
        var dto = Assert.IsType<RoundDto>(ok.Value);

        Assert.Equal("Processed", dto.Status);
        Assert.Equal(8, dto.Battles.Count);
        Assert.Equal(8, dto.Standings!.Sum(s => s.Wins));
    }

    [Fact]
    public void GetRound_WhenPending_ReturnsPairingsWithoutStandings()
    {
        var ok = Assert.IsType<OkObjectResult>(_controller.GetRound(_tournament.Id, 2));
        var dto = Assert.IsType<RoundDto>(ok.Value);

        Assert.Equal("Pending", dto.Status);
        Assert.All(dto.Battles, b => Assert.Null(b.Outcome));
        Assert.Null(dto.Standings);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(16)]
    public void GetRound_WhenRoundOutOfRange_Returns404(int roundNumber)
    {
        Assert.IsType<NotFoundObjectResult>(_controller.GetRound(_tournament.Id, roundNumber));
    }

    [Fact]
    public void GetRound_WhenTournamentMissing_Returns404()
    {
        Assert.IsType<NotFoundObjectResult>(_controller.GetRound(Guid.NewGuid(), 1));
    }

    [Fact]
    public void GetBattle_ReturnsOneBattlesResult()
    {
        var ok = Assert.IsType<OkObjectResult>(_controller.GetBattle(_tournament.Id, 3));
        var dto = Assert.IsType<BattleDto>(ok.Value);

        Assert.Equal(3, dto.Id);
        Assert.Equal(1, dto.RoundNumber);
        Assert.Equal("FirstWins", dto.Outcome);
        Assert.Equal("BaseExperience", dto.Reason);
        Assert.Equal(dto.First.Id, dto.WinnerId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(121)]
    public void GetBattle_WhenBattleOutOfRange_Returns404(int battleId)
    {
        Assert.IsType<NotFoundObjectResult>(_controller.GetBattle(_tournament.Id, battleId));
    }

    [Fact]
    public void GetBattle_WhenTournamentMissing_Returns404()
    {
        Assert.IsType<NotFoundObjectResult>(_controller.GetBattle(Guid.NewGuid(), 1));
    }

    [Fact]
    public void GetHistory_ListsTournamentSummariesWithLeaders()
    {
        var fresh = Tournament.Create(Guid.NewGuid(), DateTimeOffset.UtcNow, TournamentTests.CreateParticipants());
        _service.Setup(s => s.GetHistory()).Returns(new[] { fresh, _tournament });

        var ok = Assert.IsType<OkObjectResult>(_controller.GetHistory());
        var history = Assert.IsAssignableFrom<IReadOnlyList<TournamentSummaryDto>>(ok.Value);

        Assert.Equal(new[] { fresh.Id, _tournament.Id }, history.Select(h => h.Id));
        Assert.Empty(history[0].Leaders);
        Assert.Equal(1, history[1].RoundsProcessed);
        Assert.Equal(15, history[1].TotalRounds);
        Assert.Equal("InProgress", history[1].Status);
        Assert.Equal(8, history[1].Leaders.Count);
        Assert.All(history[1].Leaders, l => Assert.Equal(1, l.Wins));
    }
}
