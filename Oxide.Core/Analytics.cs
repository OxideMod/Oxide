using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Core
{
    public static class Analytics
    {
        private static readonly WebRequests Webrequests = Interface.Oxide.GetLibrary<WebRequests>();
        private static readonly PluginManager PluginManager = Interface.Oxide.RootPluginManager;
        private static readonly Lang Lang = Interface.Oxide.GetLibrary<Lang>();

        private const string trackingId = "UA-48448359-3";
        private const string url = "https://www.google-analytics.com/collect";

        private static Plugin[] Plugins() => PluginManager.GetPlugins().Where(pl => !pl.IsCorePlugin).ToArray();

        private static IEnumerable<string> PluginNames() => new HashSet<string>(Plugins().Select(pl => pl.Name));

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>
        {
            { "dimension1", IntPtr.Size == 8 ? "x64" : "x86" }, // CPU architecture
            { "dimension2", Environment.OSVersion.Platform.ToString().ToLower() }, // OS platform
            { "dimension3", Environment.OSVersion.Version.ToString().ToLower() }, // OS version
            { "dimension4", Utility.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLower() }, // Game name
            { "dimension5", Plugins().Length.ToString() }, // Plugin count
            { "dimension6", string.Join(", ", PluginNames().ToArray()) } // Plugin names
        };

        public static void Payload(string state)
        {
            var payload = $"v=1&tid={trackingId}&t=screenview";
            payload += $"&an=Oxide/{Environment.OSVersion}&av={OxideMod.Version}&ul={Lang.GetServerLanguage()}";
            payload += $"&cid={Environment.MachineName}{Environment.ProcessorCount}";
            payload += string.Join("", Environment.GetLogicalDrives()).Replace(":", "").Replace("\\", "").Replace("/", "")/* + "&"*/;
            //payload += string.Join("&", Tags.Select(kv => kv.Key + "=" + kv.Value).ToArray());
            Interface.Oxide.LogWarning(payload);

            Collect(payload);
        }

        public static void Collect(string payload)
        {
            var headers = new Dictionary<string, string> {{ "User-Agent", $"Oxide/{OxideMod.Version}" }};

            Webrequests.EnqueuePost(url, Uri.EscapeUriString(payload), (code, response) => { }, null, headers);
        }
    }
}
