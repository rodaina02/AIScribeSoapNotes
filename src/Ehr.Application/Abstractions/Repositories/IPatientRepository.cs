using Ehr.Application.DTOs.Patients;
using Ehr.Domain.Entities;

namespace Ehr.Application.Abstractions.Repositories;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default);

    /// <summary>Active patients for the documentation picker, ordered by name.</summary>
    Task<IReadOnlyList<PatientSessionOptionDto>> ListActiveForPickerAsync(
        CancellationToken cancellationToken = default);
}
