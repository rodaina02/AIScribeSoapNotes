using Ehr.Application.DTOs.AI;

namespace Ehr.Application.Abstractions.AI;

/// <summary>Contract for SOAP generation and code suggestion. The registered implementation calls the OpenAI chat API.</summary>
public interface IAIService
{
    Task<SoapGenerationResult> GenerateSoapAsync(
        SoapGenerationRequest request,
        CancellationToken cancellationToken = default);

    Task<MedicalCodingResult> ExtractCodesAsync(
        MedicalCodingRequest request,
        CancellationToken cancellationToken = default);
}
