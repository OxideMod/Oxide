namespace Oxide.Game.RustLegacy
{
    public static class ExtensionMethods
    {
        public static string QuoteSafe(this string str) => "\"" + str.Replace("\"", "\\\"").TrimEnd('\\') + "\"";
    }
}
