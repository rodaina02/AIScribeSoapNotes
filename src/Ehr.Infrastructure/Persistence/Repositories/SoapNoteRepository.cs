using Ehr.Application.Abstractions.Repositories;
using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ehr.Infrastructure.Persistence.Repositories;

public sealed class SoapNoteRepository(EhrDbContext db) : ISoapNoteRepository
{
    public Task<SoapNote?> GetByIdAsync(Guid tenantId, Guid soapNoteId, CancellationToken cancellationToken = default) =>
        db.SoapNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.Id == soapNoteId, cancellationToken);

    public Task<SoapNote?> GetByEncounterIdAsync(
        Guid tenantId,
        Guid encounterId,
        CancellationToken cancellationToken = default) =>
        db.SoapNotes
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.EncounterId == encounterId, cancellationToken);
}
