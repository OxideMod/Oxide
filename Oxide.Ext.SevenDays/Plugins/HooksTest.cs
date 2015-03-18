// Reference: Oxide.Ext.SevenDays

using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Wulfspider", "0.2.2", ResourceId = 826)]
    public class HooksTest : SevenDaysPlugin
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
            // TODO: LoadDefaultConfig();
        }

        public void Loaded()
        {

        }

        private void LoadDefaultConfig()
        {
            HookCalled("LoadDefaultConfig");
            // TODO: CreateDefaultConfig();
            //LoadConfig();
        }

        private void Unloaded()
        {
            HookCalled("Unloaded");
            // TODO: Unload plugin and store state in config
        }

        private void OnInitLogging()
        {
            HookCalled("OnInitLogging");
        }

        private void OnServerInitialized()
        {
            HookCalled("OnServerInitialized");
        }

        private void OnServerSave()
        {
            HookCalled("OnServerSave");
        }

        private void OnServerQuit()
        {
            HookCalled("OnServerQuit");
        }

        private void OnPlayerConnected(ClientInfo client)
        {
            HookCalled("OnPlayerConnected");
            PrintWarning("{0} has connected!", client.playerName);
            PrintToChat(client.playerName + " has connected!");
        }

        private void OnPlayerDisconnected(ClientInfo client)
        {
            HookCalled("OnPlayerDisconnected");
            PrintWarning("{0} has disconnected!", client.playerName);
            PrintToChat(client.playerName + " has disconnected!");
        }

        private void OnPlayerChat(string message, int team, string playerName)
        {
            HookCalled("OnPlayerChat");
        }

        private void OnEntityDeath(cl0006 entity)
        {
            HookCalled("OnEntityDeath");
            PrintWarning("{0} has died!", entity.EntityName);
            PrintToChat(entity.EntityName + " has died!");
        }

        private void OnEntitySpawned(Entity entity)
        {
            HookCalled("OnEntitySpawned");
            //PrintWarning("{0} has spawned!", entity._entity);
            //PrintToChat(entity._entity + " has spawned!");
        }

        private void OnEntityHurt(cl0006 entity, DamageSource source)
        {
            HookCalled("OnEntityHurt");
        }

        private void OnRunCommand(ConsoleSdtd console, ConsoleCommand command, String[] args)
        {
            string[] commandAliases = command.Names();
            Puts("The command that was executed is " + command.ToString());
            Puts("This command is called with: ");
            foreach (string alias in commandAliases)
                Puts(alias);
            Puts("This command does the following: ");
            Puts(command.Description());

            Puts("You have used " + args.Length.ToString() + " arguments with this command: ");
            foreach (string argss in args)
                Puts(argss);

            console.SendResult("This is a test reply from Oxide to the console!!");

            HookCalled("OnRunCommand");
        }
    }
}
