using System;
using System.Collections.Generic;
using System.Text;

namespace Infrastructure.Events.Mappings.MySQL
{
    public static class ExtractionExtensions
    {
        public static string ExtractValue(this string value)
        {
            if (value.Contains('\''))
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
}
