namespace LoadTest.Helpers;

public static class ListExtensions
{
    public static bool AnyFoundIn(this List<string>? keywords, string? text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        if (keywords?.Any() != true || string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        return keywords.Any(keyword => text.Contains(keyword, comparisonType));
    }

    public static IEnumerable<string> WhereFoundIn(this List<string>? keywords, string? text, StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
    {
        if (keywords?.Any() != true || string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return keywords.Where(keyword => text.Contains(keyword, comparisonType));
    }
}
