using Ehr.Domain.Entities;
using Ehr.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class EncounterMedicalCodeConfiguration : IEntityTypeConfiguration<EncounterMedicalCode>
{
    public void Configure(EntityTypeBuilder<EncounterMedicalCode> b)
    {
        b.ToTable("encounter_medical_codes", t =>
        {
            t.HasCheckConstraint("ck_enc_mc_role", "role >= 0 AND role <= 3");
            t.HasCheckConstraint("ck_enc_mc_source", "source >= 0 AND source <= 2");
            t.HasCheckConstraint(
                "ck_enc_mc_conf",
                "confidence IS NULL OR (confidence >= 0.0 AND confidence <= 1.0)");
        });

        b.HasKey(c => c.Id);
        b.Property(c => c.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(c => c.Source).IsRequired().HasDefaultValue(MedicalCodeSource.Ai);
        b.Property(c => c.SortOrder).IsRequired().HasDefaultValue(0);
        b.Property(c => c.Modifier1).HasMaxLength(2);
        b.Property(c => c.Modifier2).HasMaxLength(2);

        b.HasIndex(c => new { c.EncounterId, c.SortOrder, c.Role });
        b.HasIndex(c => new { c.TenantId, c.EncounterId });
        b.HasIndex(c => c.MedicalCodeId);

        b.HasOne(c => c.Encounter)
            .WithMany()
            .HasForeignKey(c => c.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne(c => c.MedicalCode)
            .WithMany()
            .HasForeignKey(c => c.MedicalCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(c => c.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
