using Ehr.Application.DTOs.AI;

namespace Ehr.Application.Abstractions.Coding;

public interface IMedicalCodingService
{
    /// <summary>Suggests ICD-10-CM and CPT lines for clinician review.</summary>
    Task<MedicalCodingResult> SuggestCodesAsync(
        MedicalCodingWorkflowInput input,
        CancellationToken cancellationToken = default);
}
