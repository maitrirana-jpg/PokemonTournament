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

        #endregion

        private NotFoundObjectResult TournamentNotFound() =>
            NotFound(new { error = "Tournament not found." });
    }
}
