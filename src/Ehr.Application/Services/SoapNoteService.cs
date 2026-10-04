using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.Soap;

namespace Ehr.Application.Services;

public sealed class SoapNoteService(ISoapNoteRepository soapNotes) : ISoapNoteService
{
    public async Task<SoapNoteResponse?> GetByIdAsync(
        Guid tenantId,
        Guid soapNoteId,
        CancellationToken cancellationToken = default)
    {
        var entity = await soapNotes.GetByIdAsync(tenantId, soapNoteId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new SoapNoteResponse
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            EncounterId = entity.EncounterId,
            Subjective = entity.Subjective,
            Objective = entity.Objective,
            Assessment = entity.Assessment,
            Plan = entity.Plan,
            ChiefComplaintNarrative = entity.ChiefComplaintNarrative,
            IsSigned = entity.IsSigned,
        };
    }
}
