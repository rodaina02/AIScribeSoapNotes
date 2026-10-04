using Ehr.Domain.Common;
using Ehr.Domain.Enums;

namespace Ehr.Domain.Entities;

/// <summary>
/// A single patient–provider encounter.
/// </summary>
public class Encounter : BaseEntity
{
    public Guid PatientId { get; set; }
    public Patient? Patient { get; set; }

    /// <summary>Facility at time of service (denormalized from patient if facility changes later).</summary>
    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }

    /// <summary>Clinician of record, stored as a user id. This project does not include an identity store.</summary>
    public Guid AttendingProviderUserId { get; set; }

    public DateOnly ServiceDate { get; set; }
    public TimeOnly? ScheduledTime { get; set; }

    public EncounterStatus Status { get; set; } = EncounterStatus.Scheduled;
    public EncounterType Type { get; set; } = EncounterType.FollowUp;

    public string? ChiefComplaint { get; set; }
    public string? VisitNotes { get; set; }

    public DateTimeOffset? CheckInAt { get; set; }
    public DateTimeOffset? CheckOutAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public string? CancellationReason { get; set; }

    // Optional measurements. The documentation screen does not collect these.
    public int? BloodPressureSystolic { get; set; }
    public int? BloodPressureDiastolic { get; set; }
    public int? HeartRate { get; set; }
    public int? RespiratoryRate { get; set; }
    public decimal? TemperatureFahrenheit { get; set; }
    public int? OxygenSaturationPercent { get; set; }
    public int? PainLevel0To10 { get; set; }
    public decimal? WeightPounds { get; set; }
    public decimal? HeightInches { get; set; }
    public decimal? Bmi { get; set; }
    public DateTimeOffset? VitalsRecordedAt { get; set; }
    public Guid? VitalsRecordedByUserId { get; set; }
}
