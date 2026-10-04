namespace Ehr.Application.DTOs.Encounters;

public sealed class CreatePatientVisitRequest
{
    public string? Title { get; init; }

    /// <summary>Visit date and time from the clinician’s browser.</summary>
    public required DateTimeOffset OccurredAt { get; init; }

    public required string RawConversation { get; init; }
}
