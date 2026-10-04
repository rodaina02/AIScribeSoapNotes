namespace Ehr.Application.DTOs.Encounters;

public sealed class EncounterHistoryItemDto
{
    public required Guid EncounterId { get; init; }
    public required string Title { get; init; }
    public required DateOnly ServiceDate { get; init; }
    public TimeOnly? ScheduledTime { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? Subjective { get; init; }
    public string? Objective { get; init; }
    public string? Assessment { get; init; }
    public string? Plan { get; init; }
    public string? Transcript { get; init; }
}
