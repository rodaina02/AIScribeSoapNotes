using Ehr.Domain.Common;

namespace Ehr.Domain.Entities;

/// <summary>
/// An organization that owns patients, facilities, and encounters.
/// </summary>
public class Tenant : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;

    /// <summary>National Provider Identifier (10 digits in US).</summary>
    public string? Npi { get; set; }

    public string? TaxId { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    public string? AddressLine1 { get; set; }
    public string? AddressLine2 { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public bool IsActive { get; set; } = true;
}
