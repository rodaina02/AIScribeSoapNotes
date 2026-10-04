using Ehr.Domain.Common;
using Ehr.Domain.Enums;

namespace Ehr.Domain.Entities;

/// <summary>Person receiving care.</summary>
public class Patient : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateOnly DateOfBirth { get; set; }

    public Gender Gender { get; set; }

    public string? MedicalRecordNumber { get; set; }

    /// <summary>Optional identifier from an external system. Unique per tenant when set.</summary>
    public string? ExternalPatientId { get; set; }

    public string? Room { get; set; }
    public string? Unit { get; set; }
    public string? Bed { get; set; }

    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? ZipCode { get; set; }

    public string? EmergencyContactName { get; set; }
    public string? EmergencyContactPhone { get; set; }
    public string? EmergencyContactRelationship { get; set; }

    public DateOnly? AdmissionDate { get; set; }
    public DateOnly? DischargeDate { get; set; }

    public bool IsActive { get; set; } = true;

    public Guid FacilityId { get; set; }
    public Facility? Facility { get; set; }
}
