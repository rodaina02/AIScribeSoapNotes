using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class MedicalCodeConfiguration : IEntityTypeConfiguration<MedicalCode>
{
    public void Configure(EntityTypeBuilder<MedicalCode> b)
    {
        b.ToTable("medical_codes", t =>
        {
            t.HasCheckConstraint("ck_medical_codes_type", "type IN (1, 2)");
        });

        b.HasKey(m => m.Id);
        b.Property(m => m.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(m => m.Type).IsRequired();
        b.Property(m => m.Code).IsRequired().HasMaxLength(32);
        b.Property(m => m.Description).IsRequired();
        b.Property(m => m.Category).HasMaxLength(200);
        b.Property(m => m.IsActive).IsRequired().HasDefaultValue(true);
        b.Property(m => m.DefaultFee).HasColumnType("numeric(12,2)");
        b.Property(m => m.MedicareRate).HasColumnType("numeric(12,2)");
        b.Property(m => m.WorkRvu).HasColumnType("numeric(8,2)");

        b.HasIndex(m => new { m.TenantId, m.Type, m.IsActive });
        b.HasIndex(m => new { m.TenantId, m.Type, m.Code }).IsUnique();
        b.HasIndex(m => new { m.TenantId, m.Type, m.Code })
            .HasFilter("is_active = true")
            .HasDatabaseName("idx_medical_codes_code_search");

        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(m => m.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
