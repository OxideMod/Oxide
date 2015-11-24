using CodeHatch.Blocks.Networking.Events;
using CodeHatch.Engine.Core.Networking;
using CodeHatch.Engine.Networking;
using CodeHatch.Networking.Events.Entities;
using CodeHatch.Networking.Events.Entities.Players;
using CodeHatch.Networking.Events.Players;
using CodeHatch.Networking.Events.Social;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Hooks", "Oxide Team", 0.1)]
    [Description("Tests all of the available Oxide hooks.")]

    public class Hooks : ReignOfKingsPlugin
    {
        #region Hook Verification

        int hookCount;
        int hooksVerified;
        Dictionary<string, bool> hooksRemaining = new Dictionary<string, bool>();

        public void HookCalled(string hook)
        {
            if (!hooksRemaining.ContainsKey(hook)) return;
            hookCount--;
            hooksVerified++;
            PrintWarning($"{hook} is working");
            hooksRemaining.Remove(hook);
            PrintWarning(hookCount == 0 ? "All hooks verified!" : $"{hooksVerified} hooks verified, {hookCount} hooks remaining");
        }

        #endregion

        #region Plugin Hooks

        private void Init()
        {
            hookCount = hooks.Count;
            hooksRemaining = hooks.Keys.ToDictionary(k => k, k => true);
            PrintWarning("{0} hook to test!", hookCount);
            HookCalled("Init");
        }

        protected override void LoadDefaultConfig() => HookCalled("LoadDefaultConfig");

        private void Loaded() => HookCalled("Loaded");

        private void Unloaded() => HookCalled("Unloaded");

        private void OnFrame() => HookCalled("OnFrame");

        #endregion

        #region Server Hooks

        private void OnServerInitialized() => HookCalled("OnServerInitialized");

        private void OnServerSave() => HookCalled("OnServerSave");

        private void OnServerShutdown() => HookCalled("OnServerShutdown");

        #endregion

        #region Player Hooks

        private ConnectionError OnUserApprove(ConnectionLoginData data)
        {
            HookCalled("OnUserApprove");
            Puts("Running OnUserApprove for player " + data.PlayerName + " with SteamID " + data.PlayerId);
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

        private void OnPlayerSpawn(PlayerFirstSpawnEvent e)
        {
            HookCalled("OnPlayerSpawn");
            Puts(e.Player.DisplayName + " spawned at " + e.Position);
            if (e.AtFirstSpawn)
                NextTick( () => SendReply(e.Player, "Welcome to our server, we've noticed this is your first time here so we want to inform you that you can visit our website at oxidemod.org"));
        }

        #endregion

        #region Entity Hooks

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
            Puts(e.KillingDamage != null
                ? $"{e.Entity} was killed by {e.KillingDamage?.DamageSource} ({e.KillingDamage?.DamageTypes})"
                : $"{e.Entity} died.");
        }

        #endregion

        #region Structure Hooks

        private void OnCubePlacement(CubePlaceEvent e)
        {
            HookCalled("OnCubePlacement");

            var bp = CodeHatch.Blocks.Inventory.InventoryUtil.GetTilesetBlueprint(e.Material, e.PrefabId);
            if (bp == null) return;

            Player player = null;
            foreach (var ply in Server.ClientPlayers.Where(ply => ply.Id == e.SenderId)) {
                player = ply;
            }
            if (player != null)
                Puts(player.DisplayName + " placed a " + bp.Name + " at " + e.Position);
        }

        private void OnCubeTakeDamage(CubeDamageEvent e)
        {
            HookCalled("OnCubeTakeDamage");
            Puts($"Cube at {e.Position} took {e.Damage.Amount} damage from {e.Damage.DamageSource}");
        }

        private void OnCubeDestroyed(CubeDestroyEvent e)
        {
            HookCalled("OnCubeDestroyed");
            Puts("Cube at " + e.Position + " was destroyed");
        }

        #endregion
    }
}
