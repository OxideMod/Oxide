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
            // Called when the plugin is being loaded
            // Other plugins may or may not be present, dependant on load order
            // Other plugins WILL have been executed though, so globals exposed by them will be present
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
            // Called when the config for the plugin should be initialised
            // Only called if the config file does not already exist
            HookCalled("LoadDefaultConfig");
            // TODO: CreateDefaultConfig();
            //LoadConfig();
        }

        private void Unloaded()
        {
            // Called when the config for the plugin should be initialised
            // Only called if the config file does not already exist
            HookCalled("Unloaded");
            // TODO: Unload plugin and store state in config
        }

        private void OnInitLogging()
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Used internally by Oxide to start Unity logging
            HookCalled("OnInitLogging");
        }

        private void OnServerInitialized()
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Is called after the server startup has been completed and is awaiting connections
            HookCalled("OnServerInitialized");
        }

        private void OnServerSave()
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Is called before the server saves world and player data
            HookCalled("OnServerSave");
        }

        private void OnServerQuit()
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Is called before the server starts the shutdown sequence
            HookCalled("OnServerQuit");
        }

        private void OnPlayerConnected(ClientInfo client)
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Is called before the player object is created, but after the player has been approved to join the game
            // Can get the connection from packet.connection
            HookCalled("OnPlayerConnected");
            PrintWarning("{0} has connected!", client.playerName);
            PrintToChat(client.playerName + " has connected!");
        }

        private void OnPlayerDisconnected(ClientInfo client)
        {
            // Called from Assembly-CSharp/
            // No return behavior
            // Is called before the player object is created, but after the player has been approved to join the game
            HookCalled("OnPlayerDisconnected");
            PrintWarning("{0} has disconnected!", client.playerName);
            PrintToChat(client.playerName + " has disconnected!");
        }

        private void OnPlayerChat(string message, int team, string playerName)
        {
            // Called from 
            HookCalled("OnPlayerChat");
        }

        private void OnEntityDeath(cl0006 entity)
        {
            // Called from 
            HookCalled("OnEntityDeath");
            PrintWarning("{0} has died!", entity.EntityName);
            PrintToChat(entity.EntityName + " has died!");
        }

        private void OnEntitySpawned(Entity entity)
        {
            // Called from 
            HookCalled("OnEntitySpawned");
            //PrintWarning("{0} has spawned!", entity._entity);
            //PrintToChat(entity._entity + " has spawned!");
        }

        private void OnEntityHurt(cl0006 entity, DamageSource source)
        {
            // Called from 
            HookCalled("OnEntityHurt");
        }

        private void OnRunCommand(ConsoleCommand command, String arg, String[] args)
        {
            // Called from 
            HookCalled("OnRunCommand");
        }
    }
}
