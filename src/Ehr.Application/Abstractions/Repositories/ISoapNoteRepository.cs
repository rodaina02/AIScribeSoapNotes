using Ehr.Domain.Entities;

namespace Ehr.Application.Abstractions.Repositories;

public interface ISoapNoteRepository
{
    Task<SoapNote?> GetByIdAsync(Guid tenantId, Guid soapNoteId, CancellationToken cancellationToken = default);

    Task<SoapNote?> GetByEncounterIdAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default);
}
