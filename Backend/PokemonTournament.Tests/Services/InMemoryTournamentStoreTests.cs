using Microsoft.Extensions.Options;
using PokemonTournament.Models;
using PokemonTournament.Services;
using PokemonTournament.Tests.Models;

namespace PokemonTournament.Tests.Services;

public class InMemoryTournamentStoreTests
{
    private static Tournament CreateTournament(int minutesAfterStart = 0) =>
        Tournament.Create(
            Guid.NewGuid(),
            new DateTimeOffset(2026, 10, 8, 12, minutesAfterStart, 0, TimeSpan.Zero),
            TournamentTests.CreateParticipants(2));

    private static InMemoryTournamentStore CreateStore(int max) =>
        new(Options.Create(new TournamentOptions { MaxStoredTournaments = max }));

    [Fact]
    public void Get_ReturnsAddedTournamentOrNullForUnknownId()
    {
        var store = CreateStore(5);
        var tournament = CreateTournament();

        store.Add(tournament);

        Assert.Same(tournament, store.Get(tournament.Id));
        Assert.Null(store.Get(Guid.NewGuid()));
    }

    [Fact]
    public void GetAll_ReturnsNewestFirst()
    {
        var store = CreateStore(5);
        var first = CreateTournament(0);
        var second = CreateTournament(1);
        store.Add(first);
        store.Add(second);

        Assert.Equal(new[] { second.Id, first.Id }, store.GetAll().Select(t => t.Id));
    }

    [Fact]
    public void Add_WhenAtCapacity_EvictsOldest()
    {
        var store = CreateStore(2);
        var oldest = CreateTournament(0);
        store.Add(oldest);
        store.Add(CreateTournament(1));
        store.Add(CreateTournament(2));

        Assert.Equal(2, store.GetAll().Count);
        Assert.Null(store.Get(oldest.Id));
    }

    [Fact]
    public void Constructor_WhenCapacityBelowOne_Throws()
    {
        Assert.Throws<ArgumentException>(() => CreateStore(0));
    }
}
