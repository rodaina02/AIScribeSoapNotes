using Ehr.Application.DTOs.AI;

namespace Ehr.Application.Abstractions.Clinical;

public interface ISoapGenerationService
{
    /// <summary>Builds a SOAP note using encounter context. Uses transcript path when <see cref="SoapWorkflowInput.Transcript"/> is non-empty after cleaning.</summary>
    Task<SoapGenerationResult> GenerateAsync(
        SoapWorkflowInput input,
        bool cleanTranscriptFirst = true,
        CancellationToken cancellationToken = default);
}
