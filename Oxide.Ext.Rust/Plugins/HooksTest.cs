// Reference: Oxide.Ext.Rust

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Wulfspider", "0.2.3", ResourceId = 727)]
    public class HooksTest : RustPlugin
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
            // Called from Assembly-CSharp/Bootstrap.StartupShared
            // No return behavior
            // Used internally by Oxide to start Unity logging
            HookCalled("OnInitLogging");
        }

        private void OnTick()
        {
            // Called from Assembly-CSharp/ServerMgr.DoTick
            // No return behavior
            // Called every tick (defined by the tickrate of the server?)
            HookCalled("OnTick");
        }

        private void ModifyTags(string oldtags)
        {
            // Called from Assembly-CSharp/ServerMgr.UpdateServerInformation
            // Returning a string overrides the tags with new ones
            // Used by RustCore and abstracted into BuildServerTags
            HookCalled("ModifyTags");
            // TODO: Modify tags, either remove or add
        }

        private void BuildServerTags(IList<string> tags)
        {
            // Called from RustCore.ModifyTags
            // No return behavior
            // Add tags to the list, they will be concat'd at the end
            HookCalled("ModifyTags");
            // TODO: Print new tags
        }

        private void OnTerrainInitialized()
        {
            // Called from Assembly-CSharp/InitializePVT.Apply
            // No return behavior
            // Is called after the terrain generation process has completed
            HookCalled("OnTerrainInitialized");
        }

        private void OnServerInitialized()
        {
            // Called from Assembly-CSharp/ServerMgr
            // No return behavior
            // Is called after the server startup has been completed and is awaiting connections
            HookCalled("OnServerInitialized");
        }

        private void OnServerSave()
        {
            // Called from Assembly-CSharp/SaveRestore
            // No return behavior
            // Is called before the server saves and rotates the .sav files
            HookCalled("OnServerSave");
        }

        private void OnServerShutdown()
        {
            // Called from Assembly-CSharp/Rust.Application_Quit
            // No return behavior
            // Is called before the server starts the shutdown sequence
            HookCalled("OnServerShutdown");
        }

        private void OnRunCommand(ConsoleSystem.Arg arg)
        {
            // Called from Facepunch/ConsoleSystem.Run, Facepunch/ConsoleSystem.RunUnrestricted and Facepunch/ConsoleSystem.ClientRun
            // Return true to override Rust's command handling system
            // Useful for intercepting commands before they get to their intended target(like chat.say)
            // Used by RustCore to implement chat commands
            HookCalled("OnRunCommand");
            // TODO: Run test command
            // TODO: Print command messages
        }

        private void OnUserApprove(Network.Connection connection)
        {
            // Called from Assembly-CSharp/ConnectionAuth.OnNewConnection
            // Returning a non-null value overrides default behavior, plugin should call Reject if it does this
            // Used by RustCore and abstracted into CanClientLogin
            HookCalled("OnUserApprove");
        }

        private void CanClientLogin(Network.Connection connection)
        {
            // Called from RustCore.OnUserApprove
            // Returning true will allow the connection, returning nothing will by default allow the connection, returning anything else will reject it with an error message
            // Returning a string will use the string as the error message
            HookCalled("OnUserApprove");
        }

        private void OnPlayerConnected(Network.Message packet)
        {
            // Called from Assembly-CSharp/ServerMgr
            // No return behavior
            // Is called before the player object is created, but after the player has been approved to join the game
            // Can get the connection from packet.connection
            HookCalled("OnPlayerConnected");
            // TODO: Print player connected
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            // Called from Assembly-CSharp/ServerMgr
            // No return behavior
            // Is called before the player object is created, but after the player has been approved to join the game
            HookCalled("OnPlayerDisconnected");
            // TODO: Print player disconnected
        }

        private void OnFindSpawnPoint()
        {
            // Called from Assembly-CSharp/ServerMgr
            // Return a Assembly-CSharp / BasePlayer.SpawnPoint object to use that spawnpoint
            // Useful for controlling player spawnpoints (like making all spawns occur in a set area)
            HookCalled("OnFindSpawnPoint");
            // TODO: Print spawn point
        }

        private void OnPlayerSpawn(BasePlayer player, Network.Connection connection)
        {
            // Called when the player spawns (specifically when they click the "Respawn" button)
            // No return behavior
            // ONLY called when the player is transitioning from dead to not - dead, so not when they're waking up
            // This means it's possible for a player to connect and disconnect from a server without OnPlayerSpawn ever triggering for them
            HookCalled("OnPlayerSpawn");
            // TODO: Print spawn location
            // TODO: Print start metabolism values
            // TODO: Give admin items for testing
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            // Called before a metabolism update occurs for the specified player
            // Returning true cancels the update
            // Metabolism update consists of managing the player's temperature, health etc
            // You can use this to turn off or change certain aspects of the metabolism, either by editing values before returning, or taking complete control of the method
            // Access the player object using metabolism:GetComponent("BasePlayer")
            HookCalled("OnRunPlayerMetabolism");
            // TODO: Print new metabolism values
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            // Called from Assembly-CSharp/BasePlayer
            // Returning true cancels the attack
            // Useful for modifying an attack before it goes out
            // hitinfo.HitEntity should be the entity that this attack would hit
            HookCalled("OnPlayerAttack");
            // TODO: Print target and weapon
        }

        private void OnItemCraft(ItemCraftTask item)
        {
            // Called from Assembly-CSharp/ItemCrafter
            // Return a Assembly-CSharp / ItemCraftTask object to modify behavior
            // Is called right after an item has started crafting
            HookCalled("OnItemCraft");
            // TODO: Print item crafting
        }

        private void OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        {
            // Assembly-CSharp/Item.Modules.Deploy
            // No return behavior
            // Is called right after an item has been deployed
            HookCalled("OnItemDeployed");
            // TODO: Print item deployed
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            // Called from Assembly-CSharp/ItemContainer
            // No return behavior
            // Is called right after an item was added to a container
            // An entire stack has to be created, not just adding more wood to a wood stack for example
            HookCalled("OnItemAddedToContainer");
            // TODO: Print item added
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            // Called from Assembly-CSharp/ItemContainer
            // No return behavior
            // Is called right after an item was removed from a container
            // The entire stack has to be removed for this to be called, not just a little bit
            HookCalled("OnItemAddedToContainer");
            // TODO: Print item removed
        }

        private void OnConsumableUse(Item item)
        {
            // Called from Assembly-CSharp/Item
            // No return behavior
            // Is called right after a consumable item is used
            HookCalled("OnConsumableUse");
            // TODO: Print consumable item used
        }

        private void OnGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            // Assembly-CSharp/ResourceDispenser
            // No return behavior
            // Is called before the player is given items from a resource
            HookCalled("OnGather");
            // TODO: Print item to be gathered
        }

        private void CanOpenDoor(BasePlayer player, BaseLock door)
        //private void CanOpenDoor(BasePlayer player, CodeLock doorCode)
        //private void CanOpenDoor(BasePlayer player, KeyLock doorKey)
        {
            // Called from Assembly-CSharp/BaseLock
            // Returning true will allow door usage, nothing will by default will allow door usage, returning anything else will reject door usage
            HookCalled("CanOpenDoor");
        }

        private void OnEntityAttacked(MonoBehaviour entity, HitInfo hitInfo)
        {
            // Called from multiple places, each entity's attack handler basically
            // Returning non-null value overrides default server behavior (useful for godmode, etc.)
            // Alternatively, modify the hitinfo object to change the damage
            // It should be okay to set the damage to 0, but if you don't return non-null, the player's client will receive a damage indicator(if entity is a BasePlayer)
            // hitinfo has all kinds of useful things in it, such as hitinfo.Weapon, hitinfo.damageAmount, or hitinfo.damageType
            // Currently implemented for: BasePlayer, BaseAnimal
            HookCalled("OnEntityAttacked");
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            // Assembly-CSharp/Item.Modules.Planner
            // No return behavior
            // Called when any structure is built (walls, ceilings, stairs, etc.)
            HookCalled("OnEntityBuilt");
        }

        private void OnEntityDeath(MonoBehaviour entity, HitInfo hitInfo)
        {
            // Called from multiple places, each entity's death handler basically
            // No return behavior
            // hitinfo might be null, check it before use
            // Editing hitinfo probably has no effect
            // Currently implemented for: BasePlayer, BaseAnimal
            HookCalled("OnEntityDeath");
            // TODO: Print player died
            // TODO: Automatically respawn admin after X time
        }

        private void OnEntityEnter(TriggerBase triggerBase, BaseEntity entity)
        {
            // Called from Assembly-CSharp/TriggerBase
            // No return behavior
            // Called when an entity enters an area / zone (building privilege zone, water area, radiation zone, hurt zone, etc.)
            HookCalled("OnEntityEnter");
        }

        private void OnEntityLeave(TriggerBase triggerBase, BaseEntity entity)
        {
            // Called from Assembly-CSharp/TriggerBase
            // No return behavior
            // Called when an entity leaves an area / zone (building privilege zone, water area, radiation zone, hurt zone, etc.)
            HookCalled("OnEntityLeave");
        }

        private void OnEntitySpawn(MonoBehaviour entity)
        {
            // Called from Assembly-CSharp/BaseNetworkable
            // No return behavior
            // Called when any networked entity is spawned (including trees)
            HookCalled("OnEntitySpawn");
        }

        private void OnPlayerInit(BasePlayer player)
        {
            // Called from Assembly-CSharp/BasePlayer
            // No return behavior
            // Called when the player is initializing (after they've connected, before they wake up)
            HookCalled("OnPlayerInit");
            // TODO: Force admin to spawn/wakeup
        }

        private void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            // Called from Assembly-CSharp/chat.say
            // Returning a non-null value overrides default behavior of chat, not commands
            HookCalled("OnPlayerChat");
        }

        private void OnPlayerLoot(PlayerLoot lootInventory, BaseEntity targetEntity)
        //private void OnPlayerLoot(PlayerLoot lootInventory, BasePlayer targetPlayer)
        //private void OnPlayerLoot(PlayerLoot lootInventory, Item targetItem)
        {
            // Called from Assembly-CSharp/PlayerLoot
            // No return behavior
            // Called when the player starts looting an entity or item
            HookCalled("OnPlayerLoot");
        }

        private void OnBuildingBlockUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            // Called from Assembly-CSharp/BuildingBlock
            // Returning a BuildingGrade.Enum grade will change the grade that will be upgraded to
            // Called when a player upgrades the grade of a BuildingBlock
            HookCalled("OnBuildingBlockUpgrade");
        }

        private void OnBuildingBlockRotate(BuildingBlock block, BasePlayer player)
        {
            // Called from Assembly-CSharp/BuildingBlock
            // No return behavior
            // Called when a player rotates a BuildingBlock
            HookCalled("OnBuildingBlockRotate");
        }

        private void OnBuildingBlockDemolish(BuildingBlock block, BasePlayer player)
        {
            // Called from Assembly-CSharp/BuildingBlock
            // Return true to cancel
            // Called when a player selects DemolishImmediate from the BuildingBlock menu
            HookCalled("OnBuildingBlockDemolish");
        }
    }
}
