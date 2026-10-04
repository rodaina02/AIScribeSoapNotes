namespace Ehr.Application.Options;

public sealed class DocumentationOptions
{
    public const string SectionName = "Documentation";

    /// <summary>Clinician of record for visits created from the documentation UI.</summary>
    public Guid DefaultAttendingProviderUserId { get; set; }
}
