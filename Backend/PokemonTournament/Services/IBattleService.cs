using PokemonTournament.Enums;
using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public interface IBattleService
    {
        BattleResults FightResult(Pokemon firstpokemon, Pokemon secondpokemon);

        (BattleResults Result, BattleOutcomeReason Reason) Decide(Pokemon firstpokemon, Pokemon secondpokemon);
    }
}
