// Reference: Facepunch.ID
// Reference: Google.ProtocolBuffers

using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Oxide Team", 0.1)]
    public class HooksTest : RustLegacyPlugin
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

        private void ModifyTags(string oldtags)
        {
            HookCalled("ModifyTags");
        }

        private void BuildServerTags(IList<string> tags)
        {
            HookCalled("BuildServerTags");
        }

        int ResourceCounter = 1;
        private void OnResourceNodeLoaded(ResourceTarget resource)
        {
            HookCalled("OnResourceNodeLoaded");
            // Print resource
            if (ResourceCounter <= 10)
            {
                Puts(resource.name + " loaded at " + resource.transform.position.ToString());
                ResourceCounter++;
            }
        }

        private void OnDatablocksInitialized()
        {
            HookCalled("OnDatablocksInitialized");
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

        private void OnRunCommand(ConsoleSystem.Arg arg)
        {
            HookCalled("OnRunCommand");
            var name = arg.argUser?.displayName;
            if (name == null) name = "server console";
            var cmd = arg.Class + "." + arg.Function;
            var args = arg.ArgsStr;
            Puts("The command '" + cmd + "' was used by " + name + " with the following arguments: " + args);
        }

        private void OnUserApprove(ClientConnection connection, uLink.NetworkPlayerApproval approval, ConnectionAcceptor acceptor)
        {
            HookCalled("OnUserApprove");
        }

        private void CanClientLogin(ClientConnection connection)
        {
            HookCalled("CanClientLogin");
        }

        private void OnPlayerConnected(NetUser netuser)
        {
            HookCalled("OnPlayerConnected");
            // Print player connected in in-game chat
            PrintToChat($"The player {netuser.displayName} has connected");
        }

        private void OnPlayerDisconnected(uLink.NetworkPlayer player)
        {
            HookCalled("OnPlayerDisconnected");
            // Print player disconnected in in-game chat
            var netUser = player.GetLocalData<NetUser>();
            PrintToChat($"The player {netUser.displayName} has disconnected");
        }

        private void OnPlayerSpawn(PlayerClient client, bool useCamp, RustProto.Avatar avatar)
        {
            HookCalled("OnPlayerSpawn");
            // Print spawn location in console
            Puts("Player " + client.netUser.displayName + " spawned at " + client.lastKnownPosition + ".");
            Puts("Player did " + (useCamp ? "" : "not ") + "select a sleepingbag to spawn at.");

            // Doing stuff with the player needs to wait until the next frame
            NextTick(() =>
            {
                // Print start metabolism values in console
                var player = client.controllable;
                var character = player.character;
                var metabolism = character.GetComponent<Metabolism>();

                Puts(client.netUser.displayName + " currently has the following Metabolism values:");
                Puts("  Health: " + character.health.ToString());
                Puts("  Calories: " + metabolism.GetCalorieLevel().ToString());
                Puts("  Radiation: " + metabolism.GetRadLevel().ToString());

                // Give admin items for testing
                if (client.netUser.CanAdmin())
                {
                    var inventory = player.GetComponent<Inventory>();
                    var pref = Inventory.Slot.Preference.Define(Inventory.Slot.Kind.Belt, false, Inventory.Slot.KindFlags.Belt);
                    var item = DatablockDictionary.GetByName("Uber Hatchet");
                    inventory.AddItemAmount(item, 1, pref);
                }
            });
        }

        private void OnItemCraft(CraftingInventory inventory, BlueprintDataBlock blueprint, int amount, ulong startTime)
        {
            HookCalled("OnItemCraft");
            // Print item crafting
            var netUser = inventory.GetComponent<Character>().netUser;
            Puts(netUser.displayName + " started crafting " + blueprint.resultItem.name + " x " + amount.ToString());
        }

        private void OnItemDeployed(DeployableObject component, NetUser netUser)
        {
            HookCalled("OnItemDeployed");
            // Print item deployed
            Puts(netUser.displayName + " deployed a " + component.name);
        }

        private void OnItemAdded(Inventory inventory, int slot, IInventoryItem item)
        {
            HookCalled("OnItemAdded");
            // Print item added
            var netUser = inventory.GetComponent<Character>()?.netUser;
            if (netUser == null) return;
            Puts(item.datablock.name + " was added to inventory slot " + slot.ToString() + " owned by " + netUser.displayName);
        }

        private void OnItemRemoved(Inventory inventory, int slot, IInventoryItem item)
        {
            HookCalled("OnItemRemoved");
            // Print item removed
            var netUser = inventory.GetComponent<Character>()?.netUser;
            if (netUser == null) return;
            Puts(item.datablock.name + " was removed from inventory slot " + slot.ToString() + " owned by " + netUser.displayName);
        }

        private void OnDoorToggle(BasicDoor door, ulong timestamp, Controllable controllable)
        {
            HookCalled("OnDoorToggle");
            // Print door used
            Puts(controllable.netUser.displayName + " used the door " + door.GetInstanceID() + " owned by the player with SteamID " + door.GetComponent<DeployableObject>().ownerID);
        }

        private void ModifyDamage(TakeDamage takedamage, DamageEvent damage)
        {
            HookCalled("ModifyDamage");
        }

        private void OnHurt(TakeDamage takedamage, DamageEvent damage)
        {
            HookCalled("OnHurt");
            // Print damage taken
            var netUser = damage.victim.client.netUser;
            if (netUser == null) return;
            Puts("Player " + netUser.displayName + " took " + damage.amount + " damage!");
        }

        private void OnKilled(TakeDamage takedamage, DamageEvent damage)
        {
            HookCalled("OnKilled");
            // Print death message
            var netUser = damage.victim.client.netUser;
            if (netUser == null) return;
            Puts("Player " + netUser.displayName + " has died!");
        }

        private void OnStructureBuilt(StructureComponent component, NetUser netUser)
        {
            HookCalled("OnStructureBuilt");
            // Print structure built
            Puts(netUser.displayName + " build a " + component.name);
        }

        private void OnPlayerChat(NetUser netUser, string message)
        {
            HookCalled("OnPlayerChat");
            // Print chat
            Puts(netUser.displayName + " : " + message);
        }

        private void OnPlayerVoice(NetUser netUser, List<uLink.NetworkPlayer> listeners)
        {
            HookCalled("OnPlayerVoice");
            // Print player using voice chat.
            Puts(netUser.displayName + " is currently using voice chat.");
        }

        private void OnAirDrop(Vector3 dropLocation)
        {
            HookCalled("OnAirDrop");
            // Print airdrop location
            Puts("An airdrop is being called at location " + dropLocation.ToString());
        }

        bool OnStructureDecayCalled = false;
        private void OnStructureDecay(StructureMaster master)
        {
            HookCalled("OnStructureDecay");
            // Print structure info
            if (!OnStructureDecayCalled)
            {
                Puts("Structure at " + master.transform.position.ToString() + " owned by player with SteamID " + master.ownerID + " took decay damage.");
                OnStructureDecayCalled = true;
            }
        }

        private void OnResearchItem(ResearchToolItem<ResearchToolDataBlock> tool, IInventoryItem item)
        {
            HookCalled("OnResearchItem");
            // TODO: Complete parameters
            //var netUser = item.inventory.GetComponent<Character>()?.netUser;
            var netUser = item.inventory.GetComponent<Character>()?.netUser;
            if (netUser == null) return;
            Puts(netUser.displayName + " used " + tool.datablock.name + " on the item " + item.datablock.name);
        }

        private void OnBlueprintUse(BlueprintDataBlock bp, IBlueprintItem item)
        {
            HookCalled("OnBlueprintUse");
            // TODO: Complete parameters
            var netUser = item.inventory.GetComponent<Character>()?.netUser;
            if (netUser == null) return;
            Puts(netUser.displayName + " used the blueprint " + item.datablock.name + " to learn how to craft " + bp.resultItem.name);
        }
    }
}
