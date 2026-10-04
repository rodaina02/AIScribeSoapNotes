namespace Ehr.Application.Abstractions.Processing;

/// <summary>Normalizes ASR or pasted dialogue (reference: transcript cleanup before SOAP).</summary>
public interface IConversationProcessingService
{
    /// <summary>Trims, collapses whitespace, and light normalization. Does not call an LLM.</summary>
    string CleanTranscript(string? raw);
}
