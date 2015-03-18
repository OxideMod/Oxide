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

        private void OnTick()
        {
            HookCalled("OnTick");
        }

        private void ModifyTags(string oldtags)
        {
            HookCalled("ModifyTags");
            // TODO: Modify tags, either remove or add
        }

        private void BuildServerTags(IList<string> tags)
        {
            HookCalled("ModifyTags");
            // TODO: Print new tags
        }

        private void OnTerrainInitialized()
        {
            HookCalled("OnTerrainInitialized");
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
            // TODO: Run test command
            // TODO: Print command messages
        }

        private void OnUserApprove(Network.Connection connection)
        {
            HookCalled("OnUserApprove");
        }

        private void CanClientLogin(Network.Connection connection)
        {
            HookCalled("OnUserApprove");
        }

        private void OnPlayerConnected(Network.Message packet)
        {
            HookCalled("OnPlayerConnected");
            // TODO: Print player connected
        }

        private void OnPlayerDisconnected(BasePlayer player)
        {
            HookCalled("OnPlayerDisconnected");
            // TODO: Print player disconnected
        }

        private void OnFindSpawnPoint()
        {
            HookCalled("OnFindSpawnPoint");
            // TODO: Print spawn point
        }

        private void OnPlayerSpawn(BasePlayer player, Network.Connection connection)
        {
            HookCalled("OnPlayerSpawn");
            // TODO: Print spawn location
            // TODO: Print start metabolism values
            // TODO: Give admin items for testing
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            HookCalled("OnRunPlayerMetabolism");
            // TODO: Print new metabolism values
        }

        private void OnPlayerAttack(BasePlayer attacker, HitInfo hitInfo)
        {
            HookCalled("OnPlayerAttack");
            // TODO: Print target and weapon
        }

        private void OnItemCraft(ItemCraftTask item)
        {
            HookCalled("OnItemCraft");
            // TODO: Print item crafting
        }

        private void OnItemDeployed(Deployer deployer, BaseEntity deployedEntity)
        {
            HookCalled("OnItemDeployed");
            // TODO: Print item deployed
        }

        private void OnItemAddedToContainer(ItemContainer container, Item item)
        {
            HookCalled("OnItemAddedToContainer");
            // TODO: Print item added
        }

        private void OnItemRemovedFromContainer(ItemContainer container, Item item)
        {
            HookCalled("OnItemAddedToContainer");
            // TODO: Print item removed
        }

        private void OnConsumableUse(Item item)
        {
            HookCalled("OnConsumableUse");
            // TODO: Print consumable item used
        }

        private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            HookCalled("OnConsumeFuel");
            // TODO: Print fuel consumed
        }

        private void OnGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            HookCalled("OnGather");
            // TODO: Print item to be gathered
        }

        private void CanOpenDoor(BasePlayer player, BaseLock door)
        //private void CanOpenDoor(BasePlayer player, CodeLock doorCode)
        //private void CanOpenDoor(BasePlayer player, KeyLock doorKey)
        {
            HookCalled("CanOpenDoor");
        }

        private void OnEntityAttacked(MonoBehaviour entity, HitInfo hitInfo)
        {
            HookCalled("OnEntityAttacked");
        }

        private void OnEntityBuilt(Planner planner, GameObject gameObject)
        {
            HookCalled("OnEntityBuilt");
        }

        private void OnEntityDeath(MonoBehaviour entity, HitInfo hitInfo)
        {
            HookCalled("OnEntityDeath");
            // TODO: Print player died
            // TODO: Automatically respawn admin after X time
        }

        private void OnEntityEnter(TriggerBase triggerBase, BaseEntity entity)
        {
            HookCalled("OnEntityEnter");
        }

        private void OnEntityLeave(TriggerBase triggerBase, BaseEntity entity)
        {
            HookCalled("OnEntityLeave");
        }

        private void OnEntitySpawn(MonoBehaviour entity)
        {
            HookCalled("OnEntitySpawn");
        }

        private void OnPlayerInit(BasePlayer player)
        {
            HookCalled("OnPlayerInit");
            // TODO: Force admin to spawn/wakeup
        }

        private void OnPlayerChat(ConsoleSystem.Arg arg)
        {
            HookCalled("OnPlayerChat");
        }

        private void OnPlayerLoot(PlayerLoot lootInventory, BaseEntity targetEntity)
        //private void OnPlayerLoot(PlayerLoot lootInventory, BasePlayer targetPlayer)
        //private void OnPlayerLoot(PlayerLoot lootInventory, Item targetItem)
        {
            HookCalled("OnPlayerLoot");
        }

        private void OnPlayerInput(BasePlayer player, InputState input)
        {
            HookCalled("OnPlayerLoot");
        }

        private void OnBuildingBlockUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            HookCalled("OnBuildingBlockUpgrade");
        }

        private void OnBuildingBlockRotate(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnBuildingBlockRotate");
        }

        private void OnBuildingBlockDemolish(BuildingBlock block, BasePlayer player)
        {
            HookCalled("OnBuildingBlockDemolish");
        }
    }
}
