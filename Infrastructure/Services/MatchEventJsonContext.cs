using System.Text.Json.Serialization;
using Domain.Models;

namespace Infrastructure.Services;

/// <summary>
///     JSON Serialization Source Generator Context for improved performance
///     This provides compile-time generated serialization for FootballMatchEvent types
/// </summary>
[JsonSerializable(typeof(FootballMatchEvent))]
[JsonSerializable(typeof(List<FootballMatchEvent>))]
[JsonSerializable(typeof(FootballMatchEvent[]))]
[JsonSourceGenerationOptions(
    WriteIndented = false,
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    GenerationMode = JsonSourceGenerationMode.Default)]
public partial class MatchEventJsonContext : JsonSerializerContext
{
}