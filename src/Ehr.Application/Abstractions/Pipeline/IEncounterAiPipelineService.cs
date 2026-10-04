using Ehr.Application.DTOs.Pipeline;

namespace Ehr.Application.Abstractions.Pipeline;

/// <summary>Raw conversation → clean → SOAP → coding → database (Step 8).</summary>
public interface IEncounterAiPipelineService
{
    Task<EncounterPipelineResultDto> RunAsync(
        EncounterPipelineRequest request,
        CancellationToken cancellationToken = default);
}
