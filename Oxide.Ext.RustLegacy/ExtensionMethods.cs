namespace Oxide.RustLegacy.Plugins
{
    public static class ExtensionMethods
    {
        public static string QuoteSafe(this string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";
    }
}
