using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ehr.Infrastructure.Persistence.Configurations;

public sealed class EncounterTranscriptConfiguration : IEntityTypeConfiguration<EncounterTranscript>
{
    public void Configure(EntityTypeBuilder<EncounterTranscript> b)
    {
        b.ToTable("encounter_transcripts", tb =>
        {
            tb.HasCheckConstraint(
                "ck_transcripts_dur",
                "duration_seconds IS NULL OR duration_seconds >= 0");
            tb.HasCheckConstraint(
                "ck_transcripts_conf",
                "average_confidence IS NULL OR (average_confidence >= 0.0 AND average_confidence <= 1.0)");
            tb.HasCheckConstraint("ck_transcripts_tstat", "transcription_status >= 0 AND transcription_status <= 3");
        });

        b.HasKey(t => t.Id);
        b.Property(t => t.Id).HasDefaultValueSql("gen_random_uuid()");

        b.Property(t => t.RecordedAt).IsRequired();
        b.Property(t => t.SoapGeneratedFromThis).IsRequired().HasDefaultValue(false);

        b.HasIndex(t => new { t.EncounterId, t.RecordedAt })
            .IsDescending(false, true);
        b.HasIndex(t => new { t.TenantId, t.TranscriptionStatus, t.RecordedAt })
            .IsDescending(false, false, true);

        b.HasOne(t => t.Encounter)
            .WithMany()
            .HasForeignKey(t => t.EncounterId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasOne<Tenant>().WithMany()
            .HasForeignKey(t => t.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
