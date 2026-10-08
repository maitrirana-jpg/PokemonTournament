namespace PokemonTournament.Models;

public sealed class TournamentOptions
{
    public int DefaultParticipantCount { get; set; }
    public int MinPokemonId { get; set; }
    public int MaxPokemonId { get; set; }
    public int MaxConcurrentRequests { get; set; }
    public int MaxStoredTournaments { get; set; } = 50;
}