using Ehr.Domain.Enums;

namespace Ehr.Application.DTOs.Patients;

public sealed class PatientResponse
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
    public required Gender Gender { get; init; }
    public string? MedicalRecordNumber { get; init; }
    public required Guid FacilityId { get; init; }
    public required bool IsActive { get; init; }
}
