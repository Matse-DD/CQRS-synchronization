using MySqlX.XDevAPI.Common;

namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class MySqlExtractionExtensions
{
    public static string DetermineMySqlValue(this string incoming)// TODO REIMPLEMENT WHEN CORRECTED
    {
        if (!incoming.IsString()) return incoming;

        string sign = incoming.ExtractSign();
        string value = incoming.ExtractValue();

        value = value.Sanitize();

        return $"{sign}{value}";
    }

    public static string ExtractSign(this string incoming)
    {
        if (!incoming.IsString()) return incoming;

        int startIndexStringForLength = incoming.IndexOf('\'');

        return incoming.Substring(0, startIndexStringForLength);
    }

    public static string ExtractValue(this string incoming)
    {
        if (incoming.IsString())
        {
            return ExtractStringValue(incoming);
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
        return value.Replace("\'", "\'\'");
    }

    public static bool IsString(this string value)
    {
        return value.Contains('\'');
    }
}
