using PokemonTournament.Enums;
using PokemonTournament.Models;

namespace PokemonTournament.Services
{
    public class BattleService : IBattleService
    {
        public BattleResults FightResult(Pokemon firstpokemon, Pokemon secondpokemon)
        {
            return Decide(firstpokemon, secondpokemon).Result;
        }

        public (BattleResults Result, BattleOutcomeReason Reason) Decide(Pokemon firstpokemon, Pokemon secondpokemon)
        {
            if(IsFirstPokemonWinner(firstpokemon.Type, secondpokemon.Type))
            {
                return (BattleResults.FirstWins, BattleOutcomeReason.TypeAdvantage);
            }
            if(IsFirstPokemonWinner(secondpokemon.Type, firstpokemon.Type))
            {
                return (BattleResults.SecondWins, BattleOutcomeReason.TypeAdvantage);
            }
            if(firstpokemon.BaseExperience > secondpokemon.BaseExperience)
            {
                return (BattleResults.FirstWins, BattleOutcomeReason.BaseExperience);
            }
            if(firstpokemon.BaseExperience < secondpokemon.BaseExperience)
            {
                return (BattleResults.SecondWins, BattleOutcomeReason.BaseExperience);
            }
            else
            {
                return (BattleResults.Ties, BattleOutcomeReason.EqualBaseExperience);
            }
        }

        private bool IsFirstPokemonWinner(string firsttype, string secondtype)
        {
            firsttype = firsttype.ToLower();
            secondtype = secondtype.ToLower();


            switch (firsttype)
            {
                case "water":
                    return secondtype == "fire";

                case "fire":
                    return secondtype == "grass";

                case "grass":
                    return secondtype == "electric";

                case "electric":
                    return secondtype == "water";

                case "ghost":
                    return secondtype == "psychic";

                case "psychic":
                    return secondtype == "fighting";

                case "fighting":
                    return secondtype == "dark";

                case "dark":
                    return secondtype == "ghost";

                default:
                    return false;
            }


        }
    }
}
