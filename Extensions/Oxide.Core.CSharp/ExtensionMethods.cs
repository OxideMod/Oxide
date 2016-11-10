namespace Oxide.Plugins
{
    /// <summary>
    /// Useful extension methods which are added to base types
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Converts a string into a quote safe string
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string Quote(this string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";
    }
}
