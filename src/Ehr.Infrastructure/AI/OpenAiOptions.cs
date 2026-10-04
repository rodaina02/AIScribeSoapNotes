namespace Ehr.Infrastructure.AI;

public sealed class OpenAiOptions
{
    public const string SectionName = "OpenAI";

    public string ApiKey { get; set; } = string.Empty;

    /// <summary>Chat completion model, e.g. gpt-4o.</summary>
    public string Model { get; set; } = "gpt-4o";
}
