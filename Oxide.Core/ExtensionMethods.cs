using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Core
{
    /// <summary>
    /// Useful extension methods which are added to base types
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Returns the last portion of a path separated by slashes
        /// </summary>
        public static string Basename(this string text, string extension = null)
        {
            if (extension != null)
            {
                if (extension.Equals("*.*"))
                {
                    // Return the name excluding any extension
                    var match = Regex.Match(text, @"([^\\/]+)\.[^\.]+$");
                    if (match.Success) return match.Groups[1].Value;
                }
                else
                {
                    // Return the name excluding the given extension
                    if (extension[0] == '*') extension = extension.Substring(1);
                    return Regex.Match(text, @"([^\\/]+)\" + extension + "+$").Groups[1].Value;
                }
            }
            // No extension was given or the path has no extension, return the full file name
            return Regex.Match(text, @"[^\\/]+$").Groups[0].Value;
        }

        /// <summary>
        /// Checks if an array contains a specific item
        /// </summary>
        public static bool Contains<T>(this T[] array, T value)
        {
            foreach (var item in array)
                if (item.Equals(value)) return true;
            return false;
        }

        /// <summary>
        /// Returns the directory portion of a path separated by slashes
        /// </summary>
        public static string Dirname(this string text) => Regex.Match(text, "(.+)[\\/][^\\/]+$").Groups[1].Value;

        /// <summary>
        /// Converts PascalCase and camelCase to multiple words
        /// </summary>
        public static string Humanize(this string name) => Regex.Replace(name, @"(\B[A-Z])", " $1");

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Quote(this string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";

        /// <summary>
        /// Returns a random value from an array
        /// </summary>
        public static T Sample<T>(this T[] array) => array[Core.Random.Range(0, array.Length)];

        /// <summary>
        /// Converts a string into a sanitized string for string.Format
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Sanitize(this string str) => str.Replace("{", "{{").Replace("}", "}}");

        /// <summary>
        /// Converts a string to Title Case
        /// </summary>
        public static string Titleize(this string text)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text.Contains('_') ? text.Replace('_', ' ') : text);
        }

        /// <summary>
        /// Turns an array of strings into a sentence
        /// </summary>
        /// <param name="items"></param>
        /// <returns></returns>
        public static string ToSentence<T>(this IEnumerable<T> items)
        {
            var enumerator = items.GetEnumerator();
            if (!enumerator.MoveNext()) return string.Empty;
            var firstItem = enumerator.Current;
            if (!enumerator.MoveNext()) return firstItem?.ToString();
            var builder = new StringBuilder(firstItem?.ToString());
            var moreItems = true;
            while (moreItems)
            {
                var item = enumerator.Current;
                moreItems = enumerator.MoveNext();
                builder.Append(moreItems ? ", " : " and ");
                builder.Append(item);
            }
            return builder.ToString();
        }

        /// <summary>
        /// Shortens a string to the length specified
        /// </summary>
        /// <param name="text"></param>
        /// <param name="max"></param>
        /// <returns></returns>
        public static string Truncate(this string text, int max) => text.Length <= max ? text : text.Substring(0, max) + " ...";
    }
}
