using MySqlX.XDevAPI.Common;

namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class MySqlExtractionExtensions
{
    public static string DetermineMySqlValue(this string incoming) //TODO mogelijks niet meer nodig
    {
        if (!incoming.IsString()) return incoming;

        string sign = incoming.ExtractSign();
        object value = incoming.ExtractValue();

        //value = value.Sanitize(); //TODO enkel mij properties

        return $"{sign}{value}";
    }

    public static string ExtractSign(this string incoming)
    {
        if (!incoming.IsString()) return incoming;

        int startIndexStringForLength = incoming.IndexOf('\'');

        return incoming.Substring(0, startIndexStringForLength);
    }

    public static object ExtractValue(this string incoming)
    {
        if (incoming.IsString())
        {
            return ExtractStringValue(incoming);
        }

        if (incoming.ToUpper() == "TRUE" || incoming.ToUpper() == "FALSE")
        {
            return incoming == "true" ? 1 : 0;
        }

        return incoming;
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

    public static bool IsString(this string value)
    {
        return value.Contains('\'');
    }
}
