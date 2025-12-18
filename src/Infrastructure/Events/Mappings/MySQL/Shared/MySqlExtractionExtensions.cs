using MySqlX.XDevAPI.Common;

namespace Infrastructure.Events.Mappings.MySQL.Shared;

public static class MySqlExtractionExtensions
{
    public static string DetermineMySqlValue(this string incoming)
    {
        if (!incoming.IsString()) return incoming;
        
        string sign = incoming.ExtractSign();
        string value = incoming.ExtractValue();
        
        value = value.Sanitize();

        return $"{sign}'{value}'";
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
        else
        {
            return incoming;
        }

    }

    private static string ExtractStringValue(string incoming)
    {
        int indexFirstQuote = incoming.IndexOf('\'');
        int indexLastQuote = incoming.LastIndexOf('\'');

        int startIndexString = indexFirstQuote + 1;
        int lastIndexString = indexLastQuote - 1;

        int stringLength = indexLastQuote - lastIndexString;
        return incoming.Substring(startIndexString, stringLength);
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
