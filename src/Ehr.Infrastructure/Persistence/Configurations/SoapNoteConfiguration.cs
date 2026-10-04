using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class SoapNoteConfiguration : IEntityTypeConfiguration<SoapNote>
{
    public void Configure(EntityTypeBuilder<SoapNote> b)
    {
        b.ToTable("soap_notes");

        b.HasKey(s => s.Id);
        b.Property(s => s.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(s => s.Subjective).IsRequired().HasDefaultValue(string.Empty);
        b.Property(s => s.Objective).IsRequired().HasDefaultValue(string.Empty);
        b.Property(s => s.Assessment).IsRequired().HasDefaultValue(string.Empty);
        b.Property(s => s.Plan).IsRequired().HasDefaultValue(string.Empty);
        b.Property(s => s.IsSigned).IsRequired().HasDefaultValue(false);

        b.HasIndex(s => s.EncounterId).IsUnique();
        b.HasIndex(s => new { s.TenantId, s.UpdatedAt })
            .IsDescending(false, true);

        b.HasOne(s => s.Encounter)
            .WithOne()
            .HasForeignKey<SoapNote>(s => s.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(s => s.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
