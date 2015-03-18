using System;
using System.Globalization;

namespace Oxide.Plugins
{
    public static class ExtensionMethods
    {
        public static string Titleize(this string text)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(text);
        }

        public static string Basename(this string text)
        {
            var parts = text.Split('/');
            return parts[parts.Length - 1];
        }

        public static bool Contains(this string[] array, string value)
        {
            return Array.Exists(array, str => str == value);
        }

        public static object Sample(this object[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        public static string Sample(this string[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }

        public static int Sample(this int[] array)
        {
            return array[UnityEngine.Random.Range(0, array.Length)];
        }
    }
}
