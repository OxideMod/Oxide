using System;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Core
{
    public static class Cleanup
    {
        internal static HashSet<string> files = new HashSet<string>();
        public static void Add(string file) => files.Add(file);

        internal static void Run()
        {
            if (files == null) return;

            foreach (var file in files)
            {
                try
                {
                    if (!File.Exists(file)) continue;

                    Interface.Oxide.LogDebug("Cleanup file: {0}", file);
                    File.Delete(file);
                }
                catch (Exception)
                {
                    Interface.Oxide.LogWarning("Failed to cleanup file: {0}", file);
                }
            }
            files = null;
        }
    }
}
