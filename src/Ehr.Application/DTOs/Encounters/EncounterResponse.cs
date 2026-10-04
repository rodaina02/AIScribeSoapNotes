using Ehr.Domain.Enums;

namespace Ehr.Application.DTOs.Encounters;

public sealed class EncounterResponse
{
    public required Guid Id { get; init; }
    public required Guid TenantId { get; init; }
    public required Guid PatientId { get; init; }
    public required Guid FacilityId { get; init; }
    public required Guid AttendingProviderUserId { get; init; }
    public required DateOnly ServiceDate { get; init; }
    public required EncounterStatus Status { get; init; }
    public required EncounterType Type { get; init; }
    public string? ChiefComplaint { get; init; }
}
