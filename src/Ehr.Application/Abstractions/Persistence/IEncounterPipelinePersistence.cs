using Ehr.Application.DTOs.AI;

namespace Ehr.Application.Abstractions.Persistence;

/// <summary>Transactional save of generated SOAP and suggested codes (Infrastructure implementation).</summary>
public interface IEncounterPipelinePersistence
{
    Task<EncounterPipelinePersistenceResult> PersistAsync(
        Guid tenantId,
        Guid encounterId,
        SoapGenerationResult soap,
        MedicalCodingResult coding,
        string? rawTranscript,
        CancellationToken cancellationToken = default);
}

public sealed class EncounterPipelinePersistenceResult
{
    public required Guid SoapNoteId { get; init; }
    public int MedicalCodeLinksCreated { get; init; }
    public int MedicalCodesCreated { get; init; }
}
