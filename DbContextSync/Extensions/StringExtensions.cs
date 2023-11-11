using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.Text;

namespace DbContextSync.Extensions
{
    public static class StringCaselessExtensions
    {
        private const StringComparison NO_CASE = StringComparison.OrdinalIgnoreCase;

        public static string ToCamelCase(this string s)
        {
            return s.Length > 0 ? s[..1].ToLower() + s[1..] : s;
        }

        public static string ToProperCase(this string s)
        {
            return s.Length > 0 ? s[..1].ToUpper() + s[1..] : s;
        }

        public static string Quote(this string s, char quoteMark)
        {
            return $"{quoteMark}{s}{quoteMark}";
        }

        public static string ReplaceInString(this string s, TextSpan span, string newValue)
        {
            return s.ReplaceInString(span.Start, span.End, newValue);
        }

        public static string ReplaceInString(this string s, int start, int end, string newValue)
        {
            return s.Remove(start, end - start).Insert(start, newValue);
        }

        public static bool EqualsNoCase(this string? s, string? compareTo)
        {
            return string.Equals(s, compareTo, NO_CASE);
        }

        public static bool NotEqualNoCase(this string? s, string? compareTo)
        {
            return !s.EqualsNoCase(compareTo);
        }

        public static bool StartsNoCase(this string? s, string value)
        {
            return s?.StartsWith(value, NO_CASE) ?? false;
        }

        public static bool StartsNoCase(this string? s, List<string> possibleValues)
        {
            return s.StartsNoCase(possibleValues, out _);
        }

        public static bool StartsNoCase(this string? s, List<string> possibleValues, [NotNullWhen(true)] out string? firstStartingValue)
        {
            foreach (string value in possibleValues)
            {
                if (s.StartsNoCase(value))
                {
                    firstStartingValue = value;
                    return true;
                }
            }
            firstStartingValue = null;
            return false;
        }

        public static bool EndsNoCase(this string? s, string value)
        {
            return s?.EndsWith(value, NO_CASE) ?? false;
        }

        public static bool EndsNoCase(this string? s, List<string> possibleValues)
        {
            return s.EndsNoCase(possibleValues, out _);
        }

        public static bool EndsNoCase(this string? s, List<string> possibleValues, [NotNullWhen(true)] out string? firstEndingValue)
        {
            foreach (string value in possibleValues)
            {
                if (s.EndsNoCase(value))
                {
                    firstEndingValue = value;
                    return true;
                }
            }
            firstEndingValue = null;
            return false;
        }

        public static bool StartsAndEndsNoCase(this string s, char startAndEndValue)
        {
            return s.StartsAndEndsNoCase(startAndEndValue, startAndEndValue);
        }

        public static bool StartsAndEndsNoCase(this string s, char startValue, char endValue)
        {
            return s.StartsNoCase(startValue.ToString()) && s.EndsNoCase(endValue.ToString());
        }

        public static bool StartsAndEndsNoCase(this string s, string startAndEndValue)
        {
            return s.StartsAndEndsNoCase(startAndEndValue, startAndEndValue);
        }

        public static bool StartsAndEndsNoCase(this string s, string startValue, string endValue)
        {
            return s.StartsNoCase(startValue) && s.EndsNoCase(endValue);
        }

        public static int ConsoleCharacters(this string s)
        {
            return new Regex(@"\u001b\[[0-9;]*m").Replace(s, "").Length;
        }

        public static string Pad(this string s, int totalWidth)
        {
            int length = s.ConsoleCharacters();
            if (length > totalWidth)
            {
                //!! this will no work for long coloured strings!
                //return s[..(totalWidth - 3)] + "...";
                return s;
            }
            else
            {
                return $"{s}{new string(' ', totalWidth - length)}";
            }
        }
    }
}
