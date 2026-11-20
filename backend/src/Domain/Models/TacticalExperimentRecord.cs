using System.ComponentModel.DataAnnotations;

namespace Domain.Models;

/// <summary>
///     Persisted counterfactual Monte Carlo experiment record.
/// </summary>
public sealed class TacticalExperimentRecord
{
    [Key]
    [MaxLength(128)]
    public required string Id { get; init; }

    [MaxLength(255)]
    public required string UserId { get; set; }

    [MaxLength(256)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(128)]
    public string QuestionType { get; set; } = "tactical_block_comparison";

    [MaxLength(64)]
    public string Status { get; set; } = "pending"; // pending, running, completed, failed

    [MaxLength(64)]
    public string? ExecutionTier { get; set; }

    public int NSimulations { get; set; }
    public int BaseSeed { get; set; }

    public string RequestJson { get; set; } = "{}";
    public string? ResultJson { get; set; }
    public string? CommentaryTelemetryJson { get; set; }
    public string? RepresentativeSeedsJson { get; set; }

    [MaxLength(2000)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
}
