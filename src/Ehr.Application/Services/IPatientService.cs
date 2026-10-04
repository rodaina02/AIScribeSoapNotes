using Ehr.Application.DTOs.Patients;

namespace Ehr.Application.Services;

public interface IPatientService
{
    Task<PatientResponse?> GetByIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PatientSessionOptionDto>> ListForSessionPickerAsync(
        CancellationToken cancellationToken = default);
}
