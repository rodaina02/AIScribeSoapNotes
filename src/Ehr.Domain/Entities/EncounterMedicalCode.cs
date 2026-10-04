using Ehr.Domain.Common;
using Ehr.Domain.Enums;

namespace Ehr.Domain.Entities;

/// <summary>Links an encounter to resolved codes (AI- or user-attached). Replaces a loose JSON blob on the encounter.</summary>
public class EncounterMedicalCode : BaseEntity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }

    public Guid MedicalCodeId { get; set; }
    public MedicalCode? MedicalCode { get; set; }

    public EncounterMedicalCodeRole Role { get; set; }

    public MedicalCodeSource Source { get; set; } = MedicalCodeSource.Ai;

    public double? Confidence { get; set; }
    public string? Rationale { get; set; }

    /// <summary>Line order for procedures / multiple CPT lines.</summary>
    public int SortOrder { get; set; }

    public string? Modifier1 { get; set; }
    public string? Modifier2 { get; set; }
}
