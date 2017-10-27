using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;

namespace Oxide.Core
{
    public static class Analytics
    {
        private static readonly WebRequests Webrequests = Interface.Oxide.GetLibrary<WebRequests>();
        private static readonly PluginManager PluginManager = Interface.Oxide.RootPluginManager;
        private static readonly Covalence Covalence = Interface.Oxide.GetLibrary<Covalence>();
        private static readonly Lang Lang = Interface.Oxide.GetLibrary<Lang>();

        private const string trackingId = "UA-48448359-3";
        private const string url = "https://www.google-analytics.com/collect";

        /*public static string Filename = Utility.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName);

        private static Plugin[] Plugins() => PluginManager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();

        private static IEnumerable<string> PluginNames() => new HashSet<string>(Plugins().Select(pl => pl.Name));

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>
        {
            { "dimension1", IntPtr.Size == 8 ? "x64" : "x86" }, // CPU architecture
            { "dimension2", Environment.OSVersion.Platform.ToString().ToLower() }, // OS platform
            { "dimension3", Environment.OSVersion.Version.ToString().ToLower() }, // OS version
            { "dimension4", Filename.ToLower().Replace("dedicated", "").Replace("server", "").Replace("-", "").Replace("_", "")  }, // Game name
            { "dimension5", Plugins().Length.ToString() }, // Plugin count
            { "dimension6", string.Join(", ", PluginNames().ToArray()) } // Plugin names
        };*/

        private static readonly string Identifier = $"{Covalence.Server.Address}:{Covalence.Server.Port}";

        public static void Collect()
        {
            var payload = $"v=1&tid={trackingId}&cid={Identifier}&t=screenview&cd={Covalence.Game}+{Covalence.Server.Version}";
            payload += $"&an=Oxide&av={OxideMod.Version}&ul={Lang.GetServerLanguage()}";
            //payload += string.Join("&", Tags.Select(kv => kv.Key + "=" + kv.Value).ToArray());
            SendPayload(payload);
        }

        public static void Event(string category, string action)
        {
            var payload = $"v=1&tid={trackingId}&cid={Identifier}&t=event&ec={category}&ea={action}";
            SendPayload(payload);
        }

        public static void SendPayload(string payload)
        {
            var headers = new Dictionary<string, string> { { "User-Agent", $"Oxide/{OxideMod.Version} ({Environment.OSVersion}; {Environment.OSVersion.Platform})" } };
            Webrequests.Enqueue(url, Uri.EscapeUriString(payload), (code, response) => { }, null, RequestMethod.POST, headers);
        }
    }
}
