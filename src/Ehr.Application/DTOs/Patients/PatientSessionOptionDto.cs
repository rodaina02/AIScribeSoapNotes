namespace Ehr.Application.DTOs.Patients;

/// <summary>Patient row for the documentation UI picker (display names; ids used only by the client internally).</summary>
public sealed class PatientSessionOptionDto
{
    public required Guid PatientId { get; init; }
    public required Guid TenantId { get; init; }
    public required string FirstName { get; init; }
    public required string LastName { get; init; }
    public required DateOnly DateOfBirth { get; init; }
}
