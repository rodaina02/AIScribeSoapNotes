using Ehr.Application.DTOs.Soap;

namespace Ehr.Application.Services;

public interface ISoapNoteService
{
    Task<SoapNoteResponse?> GetByIdAsync(Guid tenantId, Guid soapNoteId, CancellationToken cancellationToken = default);
}
