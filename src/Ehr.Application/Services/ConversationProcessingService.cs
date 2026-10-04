using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Ehr.Application.Abstractions.Processing;

namespace Ehr.Application.Services;

public sealed partial class ConversationProcessingService : IConversationProcessingService
{
    public string CleanTranscript(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return string.Empty;
        }

        var s = raw.Normalize(NormalizationForm.FormC).Trim();
        s = s.Replace("\r\n", "\n", StringComparison.Ordinal);
        s = ReHorizontalSpace().Replace(s, " ");
        s = ReMultiNewline().Replace(s, "\n\n");
        return s.Trim();
    }

    [GeneratedRegex("[ \\t]+", RegexOptions.CultureInvariant)]
    private static partial Regex ReHorizontalSpace();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ReMultiNewline();
}
