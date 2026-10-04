namespace Ehr.Application.DTOs.Pipeline;

/// <summary>JSON body for POST …/ai-pipeline</summary>
public sealed class RunEncounterPipelineBody
{
    public required string RawConversation { get; init; }
}

public sealed class EncounterPipelineRequest
{
    public required Guid TenantId { get; init; }
    public required Guid EncounterId { get; init; }
    /// <summary>Doctor–patient dialogue or ASR text.</summary>
    public required string RawConversation { get; init; }
}

public sealed class EncounterPipelineResultDto
{
    public required Guid EncounterId { get; init; }
    public required Guid SoapNoteId { get; init; }
    public required string CleanedTranscript { get; init; }
    public required SoapSnapshotDto Soap { get; init; }
    public required MedicalCodingSnapshotDto Coding { get; init; }
    public int MedicalCodeLinksCreated { get; init; }
    public int MedicalCodesCatalogEntriesCreated { get; init; }
}

public sealed class SoapSnapshotDto
{
    public required string Subjective { get; init; }
    public required string Objective { get; init; }
    public required string Assessment { get; init; }
    public required string Plan { get; init; }
    public string? ChiefComplaint { get; init; }
}

public sealed class MedicalCodingSnapshotDto
{
    public required string PrimaryIcd10 { get; init; }
    public required IReadOnlyList<MedicalCodingLineSnapshotDto> LineItems { get; init; }
    public string AdditionalNotes { get; init; } = string.Empty;
}

public sealed class MedicalCodingLineSnapshotDto
{
    public required string CptCode { get; init; }
    public IReadOnlyList<string> Icd10Codes { get; init; } = [];
    public int Quantity { get; init; } = 1;
    public string Modifier1 { get; init; } = string.Empty;
    public string Modifier2 { get; init; } = string.Empty;
    public string Reasoning { get; init; } = string.Empty;
}
