using Microsoft.AspNetCore.Mvc;
using PokemonTournament.Models;
using PokemonTournament.Services;

namespace PokemonTournament.Controllers
{
    /// <summary>
    /// Round-by-round Tournaments. Lives beside the original
    /// GET pokemon/tournament/statistics, which is unchanged; the {id:guid}
    /// constraint keeps literal segments like "statistics" and "history" from matching.
    /// </summary>
    [ApiController]
    [Route("pokemon/tournament")]
    public class TournamentController : ControllerBase
    {
        #region Dependencies

        private readonly IRoundByRoundTournamentService _tournamentService;

        public TournamentController(IRoundByRoundTournamentService tournamentService)
        {
            _tournamentService = tournamentService;
        }

        #endregion

        #region Endpoints

        [HttpPost]
        public async Task<IActionResult> StartTournament()
        {
            var tournament = await _tournamentService.StartAsync();

            if (tournament == null)
            {
                return StatusCode(503, new { error = "PokeAPI is unavailable. Please try again later." });
            }

            return CreatedAtAction(
                nameof(GetTournament),
                new { id = tournament.Id },
                TournamentDto.From(tournament));
        }

        [HttpGet("{id:guid}")]
        public IActionResult GetTournament(Guid id)
        {
            var tournament = _tournamentService.Get(id);

            if (tournament == null)
            {
                return TournamentNotFound();
            }

            return Ok(TournamentDto.From(tournament));
        }

        [HttpGet("history")]
        public IActionResult GetHistory()
        {
            var history = _tournamentService.GetHistory()
                .Select(TournamentSummaryDto.From)
                .ToList();

            return Ok(history);
        }

        [HttpGet("{id:guid}/rounds/{roundNumber:int}")]
        public IActionResult GetRound(Guid id, int roundNumber)
        {
            var tournament = _tournamentService.Get(id);
            if (tournament == null)
            {
                return TournamentNotFound();
            }

            var round = tournament.FindRound(roundNumber);
            if (round == null)
            {
                return NotFound(new { error = "Round not found." });
            }

            return Ok(RoundDto.From(tournament, round));
        }

        [HttpGet("{id:guid}/battles/{battleId:int}")]
        public IActionResult GetBattle(Guid id, int battleId)
        {
            var tournament = _tournamentService.Get(id);
            if (tournament == null)
            {
                return TournamentNotFound();
            }

            var battle = tournament.FindBattle(battleId);
            if (battle == null)
            {
                return NotFound(new { error = "Battle not found." });
            }

            return Ok(BattleDto.From(battle));
        }

        [HttpPost("{id:guid}/rounds")]
        public IActionResult ProcessRound(Guid id, [FromQuery] int? expectedRound)
        {
            var result = _tournamentService.ProcessNextRound(id, expectedRound);

            return result.Status switch
            {
                ProcessRoundStatus.Processed =>
                    Ok(ProcessRoundResponseDto.From(result.Tournament!, result.Round!)),
                ProcessRoundStatus.NotFound => TournamentNotFound(),
                ProcessRoundStatus.AlreadyCompleted =>
                    Conflict(new { error = "Tournament is already completed." }),
                _ => Conflict(new
                {
                    error = $"Round {expectedRound} is not the next round.",
                    nextRound = result.NextRoundNumber
                })
            };
        }

        #endregion

        private NotFoundObjectResult TournamentNotFound() =>
            NotFound(new { error = "Tournament not found." });
    }
}
