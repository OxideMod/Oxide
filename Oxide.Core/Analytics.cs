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


        private static readonly Dictionary<string, string> Tags = new Dictionary<string, string>
        {
            { "arch", IntPtr.Size == 4 ? "x86" : "x64" },
            { "game", Utility.GetFileNameWithoutExtension(Process.GetCurrentProcess().MainModule.FileName).ToLower() }
        };

        public static void Payload(string state)
        {
            var core = $"v=1&tid={trackingId}&sc={state}&t=screenview";
            var oxide = $"an=Oxide/{Environment.OSVersion}&av={OxideMod.Version}&ul={Lang.GetServerLanguage()}";
            var identifier = $"uid={Environment.MachineName}-{Environment.ProcessorCount}-{string.Join("-", Environment.GetLogicalDrives())}";

            Collect($"{core}&{oxide}&{identifier}");
        }

        public static void Collect(string payload)
        {
            var headers = new Dictionary<string, string> {{ "User-Agent", $"Oxide/{OxideMod.Version}" }};

            Webrequests.EnqueuePost(url, Uri.EscapeUriString(payload), (code, response) => { }, null, headers);
        }
    }
}
