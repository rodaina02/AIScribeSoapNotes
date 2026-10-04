using Ehr.Application.Abstractions.Persistence;
using Ehr.Application.DTOs.AI;
using Ehr.Domain.Entities;
using Ehr.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Ehr.Infrastructure.Persistence;

public sealed class EncounterPipelinePersistence(
    EhrDbContext db,
    ILogger<EncounterPipelinePersistence> log) : IEncounterPipelinePersistence
{
    public async Task<EncounterPipelinePersistenceResult> PersistAsync(
        Guid tenantId,
        Guid encounterId,
        SoapGenerationResult soap,
        MedicalCodingResult coding,
        string? rawTranscript,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        int catalogCreated = 0;
        var codeKeyCache = new Dictionary<string, Guid>(StringComparer.Ordinal);

        var existing = await db.SoapNotes
            .FirstOrDefaultAsync(
                s => s.TenantId == tenantId && s.EncounterId == encounterId, cancellationToken);
        if (existing is null)
        {
            existing = new SoapNote
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EncounterId = encounterId,
                CreatedAt = now,
            };
            db.SoapNotes.Add(existing);
        }

        existing.Subjective = soap.Subjective;
        existing.Objective = soap.Objective;
        existing.Assessment = soap.Assessment;
        existing.Plan = soap.Plan;
        existing.ChiefComplaintNarrative = soap.ChiefComplaint;
        existing.UpdatedAt = now;
        if (existing.CreatedAt == default)
        {
            existing.CreatedAt = now;
        }

        await db.EncounterMedicalCodes
            .Where(
                x => x.TenantId == tenantId
                    && x.EncounterId == encounterId
                    && x.Source == MedicalCodeSource.Ai)
            .ExecuteDeleteAsync(cancellationToken);

        var linkCount = 0;
        var sort = 0;

        if (IsCode(coding.PrimaryIcd10))
        {
            var (primaryId, n0) = await GetOrCreateCodeIdAsync(
                codeKeyCache,
                tenantId,
                MedicalCodeType.Icd10Cm,
                coding.PrimaryIcd10,
                "Primary (AI)",
                now,
                cancellationToken);
            catalogCreated += n0;
            AddCodeLink(encounterId, tenantId, primaryId, EncounterMedicalCodeRole.PrimaryDiagnosis, sort++, "Primary diagnosis (AI)", now);
            linkCount++;
        }

        var lineIndex = 0;
        foreach (var line in coding.LineItems)
        {
            if (!IsCode(line.CptCode))
            {
                continue;
            }

            var (cpt, n1) = await GetOrCreateCodeIdAsync(
                codeKeyCache,
                tenantId,
                MedicalCodeType.Cpt,
                line.CptCode,
                $"CPT line {lineIndex + 1} (AI)",
                now,
                cancellationToken);
            catalogCreated += n1;
            var cptRow = new EncounterMedicalCode
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EncounterId = encounterId,
                MedicalCodeId = cpt,
                Role = EncounterMedicalCodeRole.Procedure,
                Source = MedicalCodeSource.Ai,
                SortOrder = sort++,
                Rationale = string.IsNullOrWhiteSpace(line.Reasoning) ? "AI suggested" : line.Reasoning,
                Modifier1 = NullIfEmpty(line.Modifier1),
                Modifier2 = NullIfEmpty(line.Modifier2),
                CreatedAt = now,
                UpdatedAt = now,
            };
            db.EncounterMedicalCodes.Add(cptRow);
            linkCount++;

            foreach (var dxCode in line.Icd10Codes)
            {
                if (!IsCode(dxCode))
                {
                    continue;
                }

                var (dxMed, n2) = await GetOrCreateCodeIdAsync(
                    codeKeyCache,
                    tenantId,
                    MedicalCodeType.Icd10Cm,
                    dxCode,
                    $"Support for CPT {line.CptCode} (AI)",
                    now,
                    cancellationToken);
                catalogCreated += n2;
                AddCodeLink(
                    encounterId,
                    tenantId,
                    dxMed,
                    EncounterMedicalCodeRole.Supporting,
                    sort++,
                    $"AI — line {lineIndex + 1}",
                    now);
                linkCount++;
            }

            lineIndex++;
        }

        if (!string.IsNullOrWhiteSpace(rawTranscript))
        {
            var wordCount = rawTranscript
                .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)
                .Length;
            db.EncounterTranscripts.Add(
                new EncounterTranscript
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    EncounterId = encounterId,
                    RawText = rawTranscript.Trim(),
                    RecordedAt = now,
                    WordCount = wordCount,
                    TranscriptionService = TranscriptionServiceKind.Manual,
                    TranscriptionStatus = TranscriptionStatus.Completed,
                    SoapGeneratedFromThis = true,
                    CreatedAt = now,
                    UpdatedAt = now,
                });
        }

        await db.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        log.LogDebug("Pipeline persist: encounter {EncounterId}, soap {SoapId}, links {Links}", encounterId, existing.Id, linkCount);
        return new EncounterPipelinePersistenceResult
        {
            SoapNoteId = existing.Id,
            MedicalCodeLinksCreated = linkCount,
            MedicalCodesCreated = catalogCreated,
        };
    }

    private void AddCodeLink(
        Guid encounterId,
        Guid tenantId,
        Guid medicalCodeId,
        EncounterMedicalCodeRole role,
        int order,
        string rationale,
        DateTimeOffset now)
    {
        db.EncounterMedicalCodes.Add(
            new EncounterMedicalCode
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                EncounterId = encounterId,
                MedicalCodeId = medicalCodeId,
                Role = role,
                Source = MedicalCodeSource.Ai,
                SortOrder = order,
                Rationale = rationale,
                CreatedAt = now,
                UpdatedAt = now,
            });
    }

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static bool IsCode(string? code) =>
        !string.IsNullOrWhiteSpace(code)
        && !string.Equals(code, "N/A", StringComparison.OrdinalIgnoreCase);

    private async Task<(Guid Id, int NewRowCount)> GetOrCreateCodeIdAsync(
        Dictionary<string, Guid> cache,
        Guid tenantId,
        MedicalCodeType type,
        string code,
        string description,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var normalized = code.Trim();
        var cacheKey = $"{(int)type}|{tenantId:N}|{normalized}";
        if (cache.TryGetValue(cacheKey, out var existingId))
        {
            return (existingId, 0);
        }

        var hit = await db.MedicalCodes
            .FirstOrDefaultAsync(
                c => c.TenantId == tenantId && c.Type == type && c.Code == normalized,
                cancellationToken);
        if (hit is not null)
        {
            cache[cacheKey] = hit.Id;
            return (hit.Id, 0);
        }

        var created = new MedicalCode
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Type = type,
            Code = normalized,
            Description = description,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
        };
        db.MedicalCodes.Add(created);
        cache[cacheKey] = created.Id;
        return (created.Id, 1);
    }
}
