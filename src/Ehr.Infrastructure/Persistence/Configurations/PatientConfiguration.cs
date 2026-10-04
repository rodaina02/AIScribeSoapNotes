using Ehr.Domain.Entities;
using Ehr.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> b)
    {
        b.ToTable("patients", t =>
        {
            t.HasCheckConstraint("ck_patients_gender", "gender >= 0 AND gender <= 3");
        });

        b.HasKey(p => p.Id);
        b.Property(p => p.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(p => p.FirstName).IsRequired().HasMaxLength(100);
        b.Property(p => p.LastName).IsRequired().HasMaxLength(100);
        b.Property(p => p.DateOfBirth).IsRequired();
        b.Property(p => p.Gender)
            .IsRequired()
            .HasDefaultValue(Gender.Unknown);

        b.Property(p => p.MedicalRecordNumber).HasMaxLength(50);
        b.Property(p => p.ExternalPatientId).HasMaxLength(100);
        b.Property(p => p.Room).HasMaxLength(50);
        b.Property(p => p.Unit).HasMaxLength(50);
        b.Property(p => p.Bed).HasMaxLength(20);
        b.Property(p => p.Phone).HasMaxLength(30);
        b.Property(p => p.Email).HasMaxLength(320);
        b.Property(p => p.City).HasMaxLength(100);
        b.Property(p => p.State).HasMaxLength(2);
        b.Property(p => p.ZipCode).HasMaxLength(10);
        b.Property(p => p.EmergencyContactName).HasMaxLength(200);
        b.Property(p => p.EmergencyContactPhone).HasMaxLength(30);
        b.Property(p => p.EmergencyContactRelationship).HasMaxLength(100);
        b.Property(p => p.IsActive).IsRequired().HasDefaultValue(true);

        b.HasIndex(p => new { p.TenantId, p.IsActive });
        b.HasIndex(p => new { p.TenantId, p.LastName, p.FirstName });
        b.HasIndex(p => new { p.TenantId, p.FacilityId });
        b.HasIndex(p => new { p.TenantId, p.ExternalPatientId })
            .IsUnique()
            .HasFilter("external_patient_id IS NOT NULL");
        b.HasIndex(p => new { p.TenantId, p.MedicalRecordNumber })
            .HasFilter("medical_record_number IS NOT NULL");

        b.HasOne(p => p.Facility)
            .WithMany()
            .HasForeignKey(p => p.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(p => p.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
