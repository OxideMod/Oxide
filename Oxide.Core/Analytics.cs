using System;
using System.Collections.Generic;
using System.Diagnostics;

using Oxide.Core.Libraries;

namespace Oxide.Core
{
    public static class Analytics
    {
        private static readonly WebRequests Webrequests = Interface.Oxide.GetLibrary<WebRequests>();
        private static readonly Lang Lang = Interface.Oxide.GetLibrary<Lang>();

        private const string trackingId = "UA-48448359-3";
        private const string url = "https://www.google-analytics.com/collect";

        public class Payload
        {
            public int v = 1;
            public string tid = trackingId;
            public string an = "Oxide/" + Environment.OSVersion;
            public string ul = Lang.GetServerLanguage();
            public string t = "screenview";
            public string cid = Environment.MachineName + Environment.ProcessorCount + Environment.GetLogicalDrives();
            public VersionNumber av = OxideMod.Version;

            public override string ToString()
            {
                return string.Format($"v={v}&tid={tid}&an={an}&cd={an}&ul={ul}&t={t}&cid={cid}&av={av}");
            }
        }

        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>
        {
            { "arch", IntPtr.Size == 4 ? "x86" : "x64" },
            { "game", Utility.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLower() }
        };

        // TODO: Collect arch and game
        // TODO: Collect plugin total and count by type

        public static void Collect()
        {
            var headers = new Dictionary<string, string> {{ "User-Agent", $"Oxide/{OxideMod.Version}" }};
            Webrequests.EnqueuePost(url, Uri.EscapeUriString(new Payload().ToString()), (code, response) =>
            {
                if (code != 200) Interface.Oxide.LogWarning($"Analytics collection was not successful: {code}");
            }, null, headers);
        }
    }
}
