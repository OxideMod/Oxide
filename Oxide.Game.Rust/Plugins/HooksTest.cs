using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Hooks Test", "Oxide Team", 0.1)]
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
            // TODO: Unload plugin and store state in config
        }

        private void ModifyTags(string oldtags)
        {
            HookCalled("ModifyTags");
            // TODO: Modify tags, either remove or add
        }

        private void BuildServerTags(IList<string> tags)
        {
            HookCalled("BuildServerTags");
            // TODO: Print new tags
        }

        private void OnFrame()
        {
            HookCalled("OnFrame");
        }

        private void OnTick()
        {
            HookCalled("OnTick");
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
            HookCalled("CanClientLogin");
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

        private void OnPlayerRespawned(BasePlayer player)
        {
            HookCalled("OnPlayerRespawned");
            // TODO: Print respawn location
            // TODO: Print start metabolism values
            // TODO: Give admin items for testing
        }

        private void OnRunPlayerMetabolism(PlayerMetabolism metabolism)
        {
            HookCalled("OnRunPlayerMetabolism");
            // TODO: Print new metabolism values
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
            HookCalled("OnItemRemovedToContainer");
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

        private void CanUseDoor(BasePlayer player, BaseLock door)
        //private void CanUseDoor(BasePlayer player, CodeLock doorCode)
        //private void CanUseDoor(BasePlayer player, KeyLock doorKey)
        {
            HookCalled("CanUseDoor");
        }

        private void OnEntityTakeDamage(MonoBehaviour entity, HitInfo hitInfo)
        {
            HookCalled("OnEntityTakeDamage");
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

        private void OnEntitySpawned(MonoBehaviour entity)
        {
            HookCalled("OnEntitySpawned");
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
            HookCalled("OnPlayerInput");
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
