using Ehr.Application.DTOs.Encounters;
using Ehr.Application.DTOs.Pipeline;

namespace Ehr.Application.Services;

public interface IEncounterService
{
    Task<EncounterResponse?> GetByIdAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EncounterHistoryItemDto>> ListHistoryForPatientAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default);

    Task<EncounterPipelineResultDto> CreateVisitAndDocumentAsync(
        Guid tenantId,
        Guid patientId,
        CreatePatientVisitRequest request,
        CancellationToken cancellationToken = default);
}
