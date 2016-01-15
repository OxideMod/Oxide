using System;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Core
{
    public static class Cleanup
    {
        private static HashSet<string> files = new HashSet<string>();
        public static void Add(string file)
        {
            files.Add(file);
        }

        public static void Run()
        {
            if (files == null) return;
            foreach (var file in files)
            {
                try
                {
                    Interface.Oxide.LogDebug("Cleanup file: {0}", file);
                    if (File.Exists(file))
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
