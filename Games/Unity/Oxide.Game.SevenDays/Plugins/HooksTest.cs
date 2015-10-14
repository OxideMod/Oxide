using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Oxide Team", 0.1)]
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
        }

        public void Loaded()
        {

        }

        protected override void LoadDefaultConfig()
        {
            HookCalled("LoadDefaultConfig");
        }

        private void Unloaded()
        {
            HookCalled("Unloaded");
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

        private void OnPlayerConnected(EntityPlayer player)
        {
            HookCalled("OnPlayerConnected");
            PrintWarning("{0} has connected!", player.EntityName);
            PrintToChat(player.EntityName + " has connected!");
        }

        private void OnPlayerDisconnected(EntityPlayer player)
        {
            HookCalled("OnPlayerDisconnected");
            PrintWarning("{0} has disconnected!", player.EntityName);
            PrintToChat(player.EntityName + " has disconnected!");
        }

        private void OnPlayerChat(string name, string message)
        {
            HookCalled("OnPlayerChat");
            PrintWarning($"{name} : {message}");
        }

        private void OnGameMessage()
        {
            PrintWarning("OnGameMessage called!");
        }

        private void OnEntitySpawned(Entity entity)
        {
            HookCalled("OnEntitySpawned");
            PrintWarning($"Spawning entity {entity}");

            var entityFightable = entity as EntityFightable;
            var name = entityFightable.EntityName;

            if (name != null)
                PrintWarning($"  Entity with id {entity.entityId} and name {name} spawned.");
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

        private void OnPlayerRespawned(EntityPlayer player, RespawnType reason)
        {
            HookCalled("OnPlayerRespawned");
            PrintWarning($"{player.EntityName} has respawned ({reason}).");
        }

        private void OnEntityTakeDamage(EntityFightable entity, DamageSource dmgSource)
        {
            HookCalled("OnEntityHurt");
            var source = (EntityFightable)GameManager.Instance.World.GetEntity(dmgSource.mdv0007());
            var dmgtype = dmgSource.GetName();
            PrintWarning($"{entity.EntityName} took {dmgtype} damage from {source.EntityName}");
        }

        private void OnExperienceGained(EntityPlayer player, uint exp)
        {
            HookCalled("OnExperienceGained");
            PrintWarning($"{player.EntityName} has gained {exp} experience.");
        }

        private void OnEntityDeath(Entity entity, DamageResponse dmgResponse)
        {
            HookCalled("OnEntityDeath");
            var source = (EntityFightable)entity.fd0005.GetEntity(dmgResponse.Source.mdv0007());
            var entityFightable = entity as EntityFightable;
            PrintWarning($"{entityFightable.EntityName}");
        }

        private void OnAirdrop(object airdrop)
        {
            HookCalled("OnAirdrop");
            PrintWarning($"Airdrop inbound! {airdrop}");
            //PrintWarning($"Airdrop inbound! The plane is flying from {airdrop.start} to {airdrop.end}");
        }

        private void OnDoorUse(TileEntitySecure door, string steamId)
        {
            HookCalled("OnDoorUse");
            PrintWarning($"A door was used by a player with the Steam ID {steamId}. Owner: {door.GetOwner()} Permission to use: {door.GetUsers().Contains(steamId)}");
        }
    }
}
