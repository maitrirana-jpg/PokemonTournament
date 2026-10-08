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
