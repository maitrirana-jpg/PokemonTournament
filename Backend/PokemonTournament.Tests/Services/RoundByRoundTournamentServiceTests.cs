using Microsoft.Extensions.Options;
using Moq;
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
