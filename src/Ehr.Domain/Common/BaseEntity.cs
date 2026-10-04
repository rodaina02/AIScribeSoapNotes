namespace Ehr.Domain.Common;

/// <summary>
/// All persisted aggregates are tenant-scoped with common audit fields.
/// For a <see cref="Entities.Tenant"/> row, <see cref="TenantId"/> must equal <see cref="Id"/>.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }

    /// <summary>Owning tenant partition key. For the tenant table, equals <see cref="Id"/>.</summary>
    public Guid TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
