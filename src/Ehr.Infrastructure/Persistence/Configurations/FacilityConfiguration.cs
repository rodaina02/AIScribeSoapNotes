using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> b)
    {
        b.ToTable("facilities");

        b.HasKey(f => f.Id);
        b.Property(f => f.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(f => f.Name).IsRequired().HasMaxLength(200);
        b.Property(f => f.Address).IsRequired();
        b.Property(f => f.City).IsRequired().HasMaxLength(100);
        b.Property(f => f.State).IsRequired().HasMaxLength(2);
        b.Property(f => f.ZipCode).IsRequired().HasMaxLength(10);
        b.Property(f => f.Phone).HasMaxLength(30);
        b.Property(f => f.ExternalFacilityId).HasMaxLength(100);
        b.Property(f => f.IsActive).IsRequired().HasDefaultValue(true);

        b.HasIndex(f => new { f.TenantId, f.IsActive });
        b.HasIndex(f => f.TenantId);
        b.HasIndex(f => f.ExternalFacilityId).HasFilter("external_facility_id IS NOT NULL");
        b.HasIndex(f => new { f.TenantId, f.Name }).IsUnique();

        b.HasOne<Tenant>().WithMany().HasForeignKey(f => f.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
