using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.Encounters;
using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ehr.Infrastructure.Persistence.Repositories;

public sealed class EncounterRepository(EhrDbContext db) : IEncounterRepository
{
    public Task<Encounter?> GetByIdAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default) =>
        db.Encounters
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == encounterId, cancellationToken);

    public Task<Encounter?> GetWithPatientAsync(Guid tenantId, Guid encounterId, CancellationToken cancellationToken = default) =>
        db.Encounters
            .AsNoTracking()
            .Include(e => e.Patient)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == encounterId, cancellationToken);

    public async Task AddAsync(Encounter encounter, CancellationToken cancellationToken = default)
    {
        db.Encounters.Add(encounter);
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EncounterHistoryItemDto>> ListHistoryByPatientAsync(
        Guid tenantId,
        Guid patientId,
        CancellationToken cancellationToken = default)
    {
        var encounters = await db.Encounters
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.PatientId == patientId)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.ServiceDate)
            .ThenByDescending(e => e.ScheduledTime)
            .Select(e => new
            {
                e.Id,
                e.ChiefComplaint,
                e.ServiceDate,
                e.ScheduledTime,
                e.CreatedAt,
            })
            .ToListAsync(cancellationToken);

        if (encounters.Count == 0)
        {
            return [];
        }

        var encounterIds = encounters.Select(e => e.Id).ToList();

        var soaps = await db.SoapNotes
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && encounterIds.Contains(s.EncounterId))
            .ToDictionaryAsync(s => s.EncounterId, cancellationToken);

        var transcripts = await db.EncounterTranscripts
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId && encounterIds.Contains(t.EncounterId))
            .Select(t => new { t.EncounterId, t.RawText, t.RecordedAt, t.CreatedAt })
            .ToListAsync(cancellationToken);

        var transcriptByEncounter = transcripts
            .GroupBy(t => t.EncounterId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(t => t.RecordedAt).ThenByDescending(t => t.CreatedAt).First().RawText);

        return encounters
            .Select(e =>
            {
                soaps.TryGetValue(e.Id, out var soap);
                transcriptByEncounter.TryGetValue(e.Id, out var transcript);
                var title = string.IsNullOrWhiteSpace(e.ChiefComplaint) ? "Office visit" : e.ChiefComplaint.Trim();
                return new EncounterHistoryItemDto
                {
                    EncounterId = e.Id,
                    Title = title,
                    ServiceDate = e.ServiceDate,
                    ScheduledTime = e.ScheduledTime,
                    CreatedAt = e.CreatedAt,
                    Subjective = soap?.Subjective,
                    Objective = soap?.Objective,
                    Assessment = soap?.Assessment,
                    Plan = soap?.Plan,
                    Transcript = transcript,
                };
            })
            .ToList();
    }
}
