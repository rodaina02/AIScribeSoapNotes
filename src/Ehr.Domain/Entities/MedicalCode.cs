using Ehr.Domain.Common;
using Ehr.Domain.Enums;

namespace Ehr.Domain.Entities;

/// <summary>
/// A billable or clinical code reference, stored in one table with <see cref="Type"/> (ICD-10-CM or CPT).
/// </summary>
public class MedicalCode : BaseEntity
{
    public MedicalCodeType Type { get; set; }

    /// <summary>Normalized code string (e.g. M79.3, 99213).</summary>
    public string Code { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;
    public string? Category { get; set; }
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    // — CPT / procedure economics (null when Type == Icd10Cm) —
    public decimal? DefaultFee { get; set; }
    public decimal? MedicareRate { get; set; }
    public decimal? WorkRvu { get; set; }
}
