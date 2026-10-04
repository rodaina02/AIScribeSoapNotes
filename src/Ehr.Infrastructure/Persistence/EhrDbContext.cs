using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ehr.Infrastructure.Persistence;

public class EhrDbContext(DbContextOptions<EhrDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<Patient> Patients => Set<Patient>();
    public DbSet<MedicalCode> MedicalCodes => Set<MedicalCode>();
    public DbSet<Encounter> Encounters => Set<Encounter>();
    public DbSet<SoapNote> SoapNotes => Set<SoapNote>();
    public DbSet<EncounterTranscript> EncounterTranscripts => Set<EncounterTranscript>();
    public DbSet<EncounterMedicalCode> EncounterMedicalCodes => Set<EncounterMedicalCode>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("public");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(EhrDbContext).Assembly);
    }
}
