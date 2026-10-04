using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.Patients;

namespace Ehr.Application.Services;

public sealed class PatientService(IPatientRepository patients) : IPatientService
{
    public async Task<PatientResponse?> GetByIdAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var entity = await patients.GetByIdAsync(tenantId, patientId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        return new PatientResponse
        {
            Id = entity.Id,
            TenantId = entity.TenantId,
            FirstName = entity.FirstName,
            LastName = entity.LastName,
            DateOfBirth = entity.DateOfBirth,
            Gender = entity.Gender,
            MedicalRecordNumber = entity.MedicalRecordNumber,
            FacilityId = entity.FacilityId,
            IsActive = entity.IsActive,
        };
    }

    public Task<IReadOnlyList<PatientSessionOptionDto>> ListForSessionPickerAsync(
        CancellationToken cancellationToken = default) =>
        patients.ListActiveForPickerAsync(cancellationToken);
}
