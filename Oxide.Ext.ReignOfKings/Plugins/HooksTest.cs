using System.Collections.Generic;
using System.Linq;

using CodeHatch.Build;
using CodeHatch.Engine.Core.Networking;
using CodeHatch.Engine.Networking;
using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Networking.Events.Entities.Players;
using CodeHatch.Networking.Events.Players;
using CodeHatch.Networking.Events.Social;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Oxide Team", 0.1)]
    public class HooksTest : ReignOfKingsPlugin
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
            Puts("Running OnServerInitialized()");
        }

        private void OnServerSave()
        {
            HookCalled("OnServerSave");
            Puts("Running OnServerSave()");
        }

        private void OnServerShutdown()
        {
            HookCalled("OnServerShutdown");
            Puts("Running OnServerShutdown()");
        }

        private ConnectionError OnUserApprove(ConnectionLoginData data)
        {
            HookCalled("OnUserApprove");
            Puts("Running OnUserApprove for player " + data.PlayerName + " with SteamID " + data.PlayerId.ToString());

            return ConnectionError.NoError;
        }

        private void OnPlayerConnected(Player player)
        {
            HookCalled("OnPlayerConnected");
            Puts(player.DisplayName + " has connected to the server.");
            PrintToChat(player.DisplayName + " has joined the server!");
            SendReply(player, "Welcome to the server {0}! We hope you enjoy your stay!", player.DisplayName);
        }

        private void OnPlayerDisconnected(Player player)
        {
            HookCalled("OnPlayerDisconnected");
            Puts(player.DisplayName + " has disconnected from the server.");
            PrintToChat(player.DisplayName + " has left the server!");
        }

        private void OnPlayerChat(PlayerEvent e)
        {
            if (e is PlayerChatEvent)
            {
                var chat = (PlayerChatEvent)e;
                HookCalled("OnPlayerChat");
                Puts(chat.PlayerName + " : " + chat.Message);
            }

            if (e is GuildMessageEvent)
            {
                var chat = (GuildMessageEvent)e;
                HookCalled("OnPlayerChat");
                Puts("[Guild: " + chat.GuildName + "] " + chat.PlayerName + " : " + chat.Message);
            }
        }
        private void OnEntityHealthChange(EntityDamageEvent e)
        {
            HookCalled("OnEntityHealthChange");
            if (e.Damage.Amount > 0)
                Puts($"{e.Entity} took {e.Damage.Amount} {e.Damage?.DamageTypes} damage from {e.Damage?.DamageSource} ({e.Damage?.Damager?.name})");


            if (e.Damage.Amount < 0)
                Puts($"{e.Entity} gained {e.Damage.Amount} health.");
        }

        private void OnEntityDeath(EntityDeathEvent e)
        {
            HookCalled("OnEntityDeath");
            if (e.KillingDamage != null)
                Puts($"{e.Entity} was killed by {e.KillingDamage?.DamageSource} ({e.KillingDamage?.DamageTypes})");
            else
                Puts($"{e.Entity} died.");
        }

        void OnPlayerSpawn(PlayerFirstSpawnEvent e)
        {
            Puts(e.Player.DisplayName + " spawned at " + e.Position.ToString());
            if (e.AtFirstSpawn)
                NextTick( () => SendReply(e.Player, "Welcome to our server, we've noticed this is your first time here so we want to inform you that you can visit our website at www.oxidemod.org"));
        }

        void OnCubePlacement(CubePlaceEvent e)
        {
            if (e.PrefabId == null || e.SenderId == null || e.Material == null || e.Position == null) return;
            var bp = CodeHatch.Blocks.Inventory.InventoryUtil.GetTilesetBlueprint(e.Material, (int)e.PrefabId);
            if (bp == null) return;

            Player player = null;
            foreach (var ply in Server.ClientPlayers)
            {
                if (ply.Id == e.SenderId)
                {
                    player = ply;
                }
            }
            if (player != null)
                Puts(player.DisplayName + " placed a " + bp.Name + " at " + e.Position.ToString());
        }

        void OnCubeTakeDamage(CubeDamageEvent e)
        {
            Puts($"Cube at {e.Position} took {e.Damage.Amount} damage from {e.Damage.DamageSource}");
        }

        void OnCubeDestroyed(CubeDestroyEvent e)
        {
            Puts("Cube at " + e.Position.ToString() + " was destroyed");
        }
    }
}
