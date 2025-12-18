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

    public static string ExtractValue(this string value)
    {
        if (value.IsString())
        {
            int indexFirstQuote = value.IndexOf('\'');
            int indexLastQuote = value.LastIndexOf('\'');

            int length = indexLastQuote - indexFirstQuote - 1;
            value = value.Substring(indexFirstQuote + 1, length);
        }

        return value;
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
