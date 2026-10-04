namespace Ehr.Application.Utilities;

public static class EncounterTitleGenerator
{
    public static string FromConversation(string? rawConversation, string fallback = "Office visit")
    {
        if (string.IsNullOrWhiteSpace(rawConversation))
        {
            return fallback;
        }

        var firstClause = rawConversation
            .Trim()
            .Split(['.', '!', '?', '\n', '\r'], 2, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();

        if (string.IsNullOrWhiteSpace(firstClause))
        {
            return fallback;
        }

        var words = firstClause.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            return fallback;
        }

        var title = string.Join(' ', words.Take(8));
        return title.Length > 80 ? string.Concat(title.AsSpan(0, 77), "…") : title;
    }
}
