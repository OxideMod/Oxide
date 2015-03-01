using System;
using System.Globalization;

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
        /// Checks if a astring array contains a specific string
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
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Returns a random value from an array of strings
        /// </summary>
        public static string Sample(this string[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        /// <summary>
        /// Returns a random value from an array of integers
        /// </summary>
        public static int Sample(this int[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }
    }
}
