namespace Ehr.Domain.Enums;

/// <summary>Lifecycle of an encounter.</summary>
public enum EncounterStatus
{
    Scheduled = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    NoShow = 4
}
