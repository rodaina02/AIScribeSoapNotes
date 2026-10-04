using Ehr.Domain.Common;

namespace Ehr.Domain.Entities;

/// <summary>SOAP documentation for one encounter.</summary>
public class SoapNote : BaseEntity
{
    public Guid EncounterId { get; set; }
    public Encounter? Encounter { get; set; }

    public string Subjective { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
    public string Assessment { get; set; } = string.Empty;
    public string Plan { get; set; } = string.Empty;

    public string? ChiefComplaintNarrative { get; set; }

    public bool IsSigned { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public Guid? SignedByUserId { get; set; }
}
