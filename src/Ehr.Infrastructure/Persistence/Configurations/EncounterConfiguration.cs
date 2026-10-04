using Ehr.Domain.Entities;
using Ehr.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class EncounterConfiguration : IEntityTypeConfiguration<Encounter>
{
    public void Configure(EntityTypeBuilder<Encounter> b)
    {
        b.ToTable("encounters", t =>
        {
            t.HasCheckConstraint("ck_encounters_status", "status >= 0 AND status <= 4");
            t.HasCheckConstraint("ck_encounters_type", "type >= 0 AND type <= 3");
            t.HasCheckConstraint(
                "ck_encounters_spo2",
                "oxygen_saturation_percent IS NULL OR (oxygen_saturation_percent >= 0 AND oxygen_saturation_percent <= 100)");
            t.HasCheckConstraint(
                "ck_encounters_pain",
                "pain_level_0_to_10 IS NULL OR (pain_level_0_to_10 >= 0 AND pain_level_0_to_10 <= 10)");
        });

        b.HasKey(x => x.Id);
        b.Property(x => x.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(x => x.ServiceDate).IsRequired();
        b.Property(x => x.ScheduledTime).HasColumnType("time(0)");
        b.Property(x => x.Status)
            .IsRequired()
            .HasDefaultValue(EncounterStatus.Scheduled);
        b.Property(x => x.Type)
            .IsRequired()
            .HasDefaultValue(EncounterType.FollowUp);

        b.Property(x => x.TemperatureFahrenheit).HasColumnType("numeric(5,1)");
        // Snake-case convention maps PainLevel0To10 -> pain_level0to10; DB column is pain_level_0_to_10
        b.Property(x => x.PainLevel0To10).HasColumnName("pain_level_0_to_10");
        b.Property(x => x.WeightPounds).HasColumnType("numeric(6,1)");
        b.Property(x => x.HeightInches).HasColumnType("numeric(5,1)");
        b.Property(x => x.Bmi).HasColumnType("numeric(5,1)");

        b.HasIndex(x => x.TenantId);
        b.HasIndex(x => new { x.TenantId, x.PatientId, x.ServiceDate })
            .IsDescending(false, false, true);
        b.HasIndex(x => new { x.TenantId, x.Status });
        b.HasIndex(x => new { x.TenantId, x.AttendingProviderUserId, x.ServiceDate })
            .IsDescending(false, false, true);
        b.HasIndex(x => new { x.TenantId, x.FacilityId });

        b.HasOne(x => x.Patient)
            .WithMany()
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Facility)
            .WithMany()
            .HasForeignKey(x => x.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
