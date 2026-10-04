using Ehr.Application.DTOs.Encounters;
using Ehr.Domain.Entities;

namespace Ehr.Application.Abstractions.Repositories;

public interface IEncounterRepository
{
    Task<Encounter?> GetByIdAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default);

    /// <summary>Encounter with patient for clinical documentation (AI pipeline).</summary>
    Task<Encounter?> GetWithPatientAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default);

    Task AddAsync(Encounter encounter, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EncounterHistoryItemDto>> ListHistoryByPatientAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default);
}
