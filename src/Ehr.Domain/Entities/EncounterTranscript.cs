using Ehr.Domain.Common;
using Ehr.Domain.Enums;

namespace Ehr.Domain.Entities;

/// <summary>
/// Captured conversation text for an encounter. Audio fields are reserved and are not filled by the current pipeline.
/// </summary>
public class EncounterTranscript : BaseEntity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }

    /// <summary>Blob or file storage key; empty if text-only / manual input.</summary>
    public string? AudioStorageKey { get; set; }

    public string? RawText { get; set; }

    public DateTimeOffset RecordedAt { get; set; }
    public int? DurationSeconds { get; set; }
    public long? FileSizeBytes { get; set; }
    public int? WordCount { get; set; }

    public TranscriptionServiceKind TranscriptionService { get; set; } = TranscriptionServiceKind.Unknown;
    public double? AverageConfidence { get; set; }
    public TranscriptionStatus TranscriptionStatus { get; set; } = TranscriptionStatus.Pending;

    public bool SoapGeneratedFromThis { get; set; }

    public Guid? CreatedByUserId { get; set; }
}
