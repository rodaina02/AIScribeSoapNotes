namespace Ehr.Domain.Enums;

public enum TranscriptionServiceKind
{
    Unknown = 0,
    Deepgram = 1,
    OpenAiWhisper = 2,
    Manual = 3,
    Azure = 4,
    Google = 5
}
