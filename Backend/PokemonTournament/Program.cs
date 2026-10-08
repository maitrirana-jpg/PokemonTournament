

using PokemonTournament.Infrastructure;
using PokemonTournament.Models;
using PokemonTournament.Services;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOptions<TournamentOptions>()
    .BindConfiguration("Tournament");

//Since this is a small project, I have added them here for simplicity.
// In a larger project, I use ServiceCollectionExtensions to register services in a separate class.
builder.Services.AddHttpClient<IPokeClient, PokeClient>();
builder.Services.AddScoped<IBattleService, BattleService>();
builder.Services.AddScoped<ITournamentService, TournamentService>();
builder.Services.AddScoped<IAlertService, LoggingAlertService>();
builder.Services.AddScoped<IRosterProvider, RosterProvider>();
builder.Services.AddScoped<IRoundByRoundTournamentService, RoundByRoundTournamentService>();
// Tournaments must outlive a request, so the store is a singleton (see docs/adr/0001).
builder.Services.AddSingleton<ITournamentStore, InMemoryTournamentStore>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHealthChecks()
    .AddCheck<PokeApiHealthCheck>("pokeapi", tags: new[] { "ready", "dependency" });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularApp", policy =>
        policy.WithOrigins(
            "http://localhost:4200",
            "http://localhost:5129",
            "https://localhost:7025")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();

        logger.LogError(
            exception,
            "Unhandled exception while processing {RequestMethod} {RequestPath}.",
            context.Request.Method,
            context.Request.Path);

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        await Results.Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "An unexpected error occurred.")
            .ExecuteAsync(context);
    });
});

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseCors("AngularApp");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();
