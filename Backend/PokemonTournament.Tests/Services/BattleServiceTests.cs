using PokemonTournament.Enums;
using PokemonTournament.Models;
using PokemonTournament.Services;

namespace PokemonTournament.Tests.Services;

public class BattleServiceTests
{
    private readonly BattleService _service = new();

    private Pokemon CreatePokemon(string type, int exp = 100)
    {
        return new Pokemon
        {
            Type = type,
            BaseExperience = exp
        };
    }

    [Fact]
    public void FightResult_WhenFirstHasTypeAdvantage_ReturnsFirstWins()
    {
        var first = CreatePokemon("water");
        var second = CreatePokemon("fire");

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.FirstWins, result);
    }

    [Fact]
    public void FightResult_WhenSecondHasTypeAdvantage_ReturnsSecondWins()
    {
        var first = CreatePokemon("grass");
        var second = CreatePokemon("fire");

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.SecondWins, result);
    }

    [Fact]
    public void FightResult_WhenFirstHasHigherBaseExperience_ReturnsFirstWins()
    {
        var first = CreatePokemon("normal", 200);
        var second = CreatePokemon("normal", 100);

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.FirstWins, result);
    }

    [Fact]
    public void FightResult_WhenSecondHasHigherBaseExperience_ReturnsSecondWins()
    {
        var first = CreatePokemon("same", 50);
        var second = CreatePokemon("same", 100);

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.SecondWins, result);
    }

    [Fact]
    public void FightResult_WhenEqualExperienceAndNoTypeAdvantage_ReturnsTie()
    {
        var first = CreatePokemon("same", 100);
        var second = CreatePokemon("same", 100);

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.Ties, result);
    }

    [Fact]
    public void FightResult_TypeComparisonIsCaseInsensitive()
    {
        var first = CreatePokemon("WATER");
        var second = CreatePokemon("fire");

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.FirstWins, result);
    }

    [Fact]
    public void FightResult_UnknownTypes_NoTypeAdvantage_UsesExperience()
    {
        var first = CreatePokemon("water1", 150);
        var second = CreatePokemon("fire1", 100);

        var result = _service.FightResult(first, second);

        Assert.Equal(BattleResults.FirstWins, result);
    }

    [Theory]
    [InlineData("water", 50, "fire", 200, BattleResults.FirstWins, BattleOutcomeReason.TypeAdvantage)]
    [InlineData("fire", 200, "water", 50, BattleResults.SecondWins, BattleOutcomeReason.TypeAdvantage)]
    [InlineData("normal", 200, "normal", 100, BattleResults.FirstWins, BattleOutcomeReason.BaseExperience)]
    [InlineData("normal", 100, "normal", 200, BattleResults.SecondWins, BattleOutcomeReason.BaseExperience)]
    [InlineData("normal", 100, "normal", 100, BattleResults.Ties, BattleOutcomeReason.EqualBaseExperience)]
    public void Decide_ReturnsSameResultAsFightResultWithItsReason(
        string firstType, int firstExp, string secondType, int secondExp,
        BattleResults expectedResult, BattleOutcomeReason expectedReason)
    {
        var first = CreatePokemon(firstType, firstExp);
        var second = CreatePokemon(secondType, secondExp);

        var (result, reason) = _service.Decide(first, second);

        Assert.Equal(expectedResult, result);
        Assert.Equal(expectedReason, reason);
        Assert.Equal(_service.FightResult(first, second), result);
    }
}
