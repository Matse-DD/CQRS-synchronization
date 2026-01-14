using Infrastructure.Events.Mappings.MySQL.Shared;

namespace Infrastructure.Events.Mappings.Shared;

public static class ExtractionMethods
{
    private static readonly IEnumerable<string> POSSIBLE_SIGNS =
    [
        "+", "-", "*", "/", "<=", ">=", "<>", "=", "<", ">"
    ];

    public static string ExtractSign(this string incoming)
    {
        if (IsString(incoming)) return ExtractSignForString(incoming);

        string? sign = POSSIBLE_SIGNS.FirstOrDefault(sign => incoming.Contains(sign));

        if (sign == null) return "";
        return sign;
    }

    private static string ExtractSignForString(string incoming)
    {
        int startIndexStringForLength = incoming.IndexOf('\'');

        return incoming.Substring(0, startIndexStringForLength);
    }

    public static object ExtractValue(this string incoming)
    {
        if (IsString(incoming))
        {
            return ExtractStringValue(incoming);
        }
        IEnumerable<string> splittedOnSign = SplitOnPossibleSigns(incoming);

        string value = splittedOnSign.Last();

        if (value.ToUpper() == "TRUE" || value.ToUpper() == "FALSE")
        {
            return value.ToUpper() == "TRUE" ? 1 : 0;
        }

        return value;
    }

    private static IEnumerable<string> SplitOnPossibleSigns(string incoming)
    {
        return incoming.Split(POSSIBLE_SIGNS.ToArray(), StringSplitOptions.None);
    }

    private static string ExtractStringValue(string incoming)
    {
        int indexFirstQuote = incoming.IndexOf('\'');
        int indexLastQuote = incoming.LastIndexOf('\'');

        int startIndexStringValue = indexFirstQuote + 1;
        int lastIndexStringValue = indexLastQuote - 1;

        int stringLength = lastIndexStringValue - indexFirstQuote;

        return incoming.Substring(startIndexStringValue, stringLength);
    }

    public static string Sanitize(this string value)
    {
        // TODO some sanitization bij properties
        return value;
    }

    public static bool IsString(string value)
    {
        return value.Contains('\'');
    }
}
