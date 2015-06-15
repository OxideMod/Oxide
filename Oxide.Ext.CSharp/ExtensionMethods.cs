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
        public static string Titleize(this string text)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        /// <summary>
        /// Returns the last portion of a path separated by slashes
        /// </summary>
        public static string Basename(this string text)
        {
            var parts = text.Split('/', '\\');
            return parts[parts.Length - 1];
        }

        /// <summary>
        /// Turns an array of strings into a sentence
        /// </summary>
        /// <param name="enumerable"></param>
        /// <returns></returns>
        public static string ToSentence(this IEnumerable<string> enumerable)
        {
            var strings = enumerable.ToArray();
            if (strings.Length < 1) return string.Empty;
            var output = strings[0];
            if (strings.Length == 1) return output;
            for (var i = 1; i < strings.Length - 1; i++)
                output += ", " + strings[i];
            output += " and " + strings[strings.Length - 1];
            return output;
        }

        /// <summary>
        /// Checks if a string array contains a specific string
        /// </summary>
        public static bool Contains(this string[] array, string value)
        {
            return Array.Exists(array, str => str == value);
        }

        /// <summary>
        /// Returns a random value from an array of objects
        /// </summary>
        public static object Sample(this object[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Returns a random value from an array of strings
        /// </summary>
        public static string Sample(this string[] array)
        {
            return array[Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Returns a random value from an array of integers
        /// </summary>
        public static int Sample(this int[] array)
        {
            return array[Random.Range(0, array.Length)];
        }
    }
}
