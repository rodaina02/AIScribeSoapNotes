using Ehr.Domain.Common;

namespace Ehr.Domain.Entities;

/// <summary>
/// Care site (e.g. SNF) where encounters occur. Kept in domain because <see cref="Patient"/> and encounters snapshot location.
/// </summary>
public class Facility : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string? Phone { get; set; }

    /// <summary>Optional identifier from an external system.</summary>
    public string? ExternalFacilityId { get; set; }

    public bool IsActive { get; set; } = true;
}
