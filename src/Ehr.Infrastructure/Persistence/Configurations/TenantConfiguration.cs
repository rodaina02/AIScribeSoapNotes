using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> b)
    {
        b.ToTable("tenants", t =>
        {
            t.HasCheckConstraint("ck_tenants_tenant_matches", "id = tenant_id");
        });

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).ValueGeneratedNever();
        b.Property(t => t.TenantId).ValueGeneratedNever();

        b.Property(t => t.Name).IsRequired().HasMaxLength(200);
        b.Property(t => t.Slug).IsRequired().HasMaxLength(200);
        b.Property(t => t.Npi).HasMaxLength(20);
        b.Property(t => t.TaxId).HasMaxLength(20);
        b.Property(t => t.Phone).HasMaxLength(30);
        b.Property(t => t.Email).HasMaxLength(320);
        b.Property(t => t.Website).HasMaxLength(2000);
        b.Property(t => t.AddressLine1).HasMaxLength(200);
        b.Property(t => t.AddressLine2).HasMaxLength(200);
        b.Property(t => t.City).HasMaxLength(100);
        b.Property(t => t.State).HasMaxLength(2);
        b.Property(t => t.ZipCode).HasMaxLength(10);
        b.Property(t => t.IsActive).IsRequired().HasDefaultValue(true);

        b.HasIndex(t => t.TenantId);
        // Hand-written SQL also creates uq_tenants_slug on lower(slug). Prefer normalized slugs in the app, or a raw migration to match.
        b.HasIndex(t => t.Slug).IsUnique();
    }
}
