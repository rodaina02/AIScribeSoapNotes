using Ehr.Application.Abstractions.Repositories;
using Ehr.Application.DTOs.Patients;
using Ehr.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ehr.Infrastructure.Persistence.Repositories;

public sealed class PatientRepository(EhrDbContext db) : IPatientRepository
{
    public Task<Patient?> GetByIdAsync(Guid tenantId, Guid patientId, CancellationToken cancellationToken = default) =>
        db.Patients
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TenantId == tenantId && p.Id == patientId && p.IsActive, cancellationToken);

    public async Task<IReadOnlyList<PatientSessionOptionDto>> ListActiveForPickerAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.Patients
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ThenByDescending(p => p.CreatedAt)
            .Select(p => new PatientSessionOptionDto
            {
                PatientId = p.Id,
                TenantId = p.TenantId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                DateOfBirth = p.DateOfBirth,
            })
            .ToListAsync(cancellationToken);
    }
}
