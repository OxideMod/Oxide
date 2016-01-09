using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Random = Oxide.Core.Random;

namespace Oxide.Plugins
{
    /// <summary>
    /// Useful extension methods which are added to base types
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a string to title case
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Titleize(this string text) => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);

        /// <summary>
        /// Returns the last portion of a path separated by slashes
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string Basename(this string text)
        {
            var parts = text.Split('/', '\\');
            return parts[parts.Length - 1];
        }

        /// <summary>
        /// Checks if a string array contains a specific string
        /// </summary>
        /// <param name="array"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool Contains(this string[] array, string value) => Array.Exists(array, str => str == value);

        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Quote(this string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";

        /// <summary>
        /// Returns a random value from an array of objects
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static object Sample(this object[] array) => array[Random.Range(0, array.Length)];

        /// <summary>
        /// Returns a random value from an array of strings
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static string Sample(this string[] array) => array[Random.Range(0, array.Length)];

        /// <summary>
        /// Returns a random value from an array of integers
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static int Sample(this int[] array) => array[Random.Range(0, array.Length)];

        /// <summary>
        /// Returns an array of strings as a sentence
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static string ToSentence(this IEnumerable<string> enumerable)
        {
            var strings = enumerable.ToArray();
            if (strings.Length < 1) return string.Empty;
            var output = strings[0];
            if (strings.Length == 1) return output;
            for (var i = 1; i < strings.Length - 1; i++) output += ", " + strings[i];
            output += " and " + strings[strings.Length - 1];
            return output;
        }
    }
}
