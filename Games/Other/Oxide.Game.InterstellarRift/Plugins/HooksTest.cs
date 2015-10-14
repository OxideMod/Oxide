using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Oxide Team", 0.1)]
    public class HooksTest : InterstellarRiftPlugin
    {
        int hookCount = 0;
        int hooksVerified;
        Dictionary<string, bool> hooksRemaining = new Dictionary<string, bool>();

        public void HookCalled(string name)
        {
            if (!hooksRemaining.ContainsKey(name)) return;
            hookCount--;
            hooksVerified++;
            PrintWarning("{0} is working. {1} hooks verified!", name, hooksVerified);
            hooksRemaining.Remove(name);
            if (hookCount == 0)
                PrintWarning("All hooks verified!");
            else
                PrintWarning("{0} hooks remaining: " + string.Join(", ", hooksRemaining.Keys.ToArray()), hookCount);
        }

        private void Init()
        {
            hookCount = hooks.Count;
            hooksRemaining = hooks.Keys.ToDictionary(k => k, k => true);
            PrintWarning("{0} hook to test!", hookCount);
            HookCalled("Init");
        }

        public void Loaded()
        {
            HookCalled("Loaded");
        }

        protected override void LoadDefaultConfig()
        {
            HookCalled("LoadDefaultConfig");
        }

        private void Unloaded()
        {
            HookCalled("Unloaded");
        }

        private void OnFrame()
        {
            HookCalled("OnFrame");
        }

        private void OnServerInitialized()
        {
            HookCalled("OnServerInitialized");
        }

        private void OnServerSave()
        {
            HookCalled("OnServerSave");
        }

        private void OnServerShutdown()
        {
            HookCalled("OnServerShutdown");
        }
    }
}
