// Reference: Assembly-UnityScript

using System.Collections.Generic;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Hooks", "Oxide Team", 0.1)]
    [Description("Tests all of the available Oxide hooks.")]

    public class Hooks : TheForestPlugin
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

        private void OnPlayerChat(ChatEvent e)
        {
            HookCalled("OnPlayerChat");
            // TODO: Show player chat
        }

        private void OnPlayerConnected(BoltConnection connection)
        {
            HookCalled("OnPlayerConnected");
            // TODO: Show player connected
        }

        private void OnPlayerDisconnected(BoltConnection connection)
        {
            HookCalled("OnPlayerDisconnected");
            // TODO: Show player disconnected
        }

        private void OnPlayerInit(BoltEntity player)
        {
            HookCalled("OnPlayerInit");
            // TODO: Show player info
        }

        private void OnPlayerSpawn(BoltEntity player)
        {
            HookCalled("OnPlayerSpawn");
            // TODO: Show player info
        }

        #endregion

        #region Entity Hooks

        private void OnEntityBurn(Burn e)
        {
            HookCalled("OnEntityBurn");
            // TODO: Show entity info
        }

        private void OnEntityFrozen(BoltEntity entity)
        {
            HookCalled("OnEntityFrozen");
            // TODO: Show entity info
        }

        private void OnEntityThaw(BoltEntity entity)
        {
            HookCalled("OnEntityThaw");
            // TODO: Show entity info
        }

        #endregion

        #region Item Hooks

        private void OnSuitcaseHit(SuitCase suitcase)
        {
            HookCalled("OnSuitcaseHit");
            // TODO: Show suitcase info
        }

        private void OnSuitcaseOpen(SuitCase suitcase)
        {
            HookCalled("OnSuitcaseOpen");
            // TODO: Show suitcase info
        }

        private void OnSuitcasePush(ClientSuitcasePush suitcase)
        {
            HookCalled("OnSuitcasePush");
            // TODO: Show suitcase info
        }

        private void OnSuitcaseSpawn(suitCaseSpawn suitcase)
        {
            HookCalled("OnSuitcaseSpawn");
            // TODO: Show suitcase info
        }

        #endregion

        private void OnBerryPick() => HookCalled("OnBerryPick");
        private void OnBlueprintCancel() => HookCalled("OnBlueprintCancel");
        private void OnBodyAdd() => HookCalled("OnBodyAdd");
        private void OnBodyTake() => HookCalled("OnBodyTake");
        private void OnConstructionPlace() => HookCalled("OnConstructionPlace");
        private void OnCorpseHit() => HookCalled("OnCorpseHit");
        private void OnCorpsePosition() => HookCalled("OnCorpsePosition");
        private void OnCrateBreak() => HookCalled("OnCrateBreak");
        private void OnCutTreeSpawn() => HookCalled("OnCutTreeSpawn");
        private void OnDestroyWithTAg() => HookCalled("OnDestroyWithTag");
        private void OnEffigyAddPart() => HookCalled("OnEffigyAddPart");
        private void OnEffigyLight() => HookCalled("OnEffigyLight");
        private void OnFireLight() => HookCalled("OnFireLight");
        private void OnFireWarmth() => HookCalled("OnFireWarmth");
        private void OnFireWarmthEnd() => HookCalled("OnFireWarmthEnd");
        private void OnFoundationExPlace() => HookCalled("OnFoundationExPlace");
        private void OnFuelAddedToFire() => HookCalled("OnFuelAddedToFire");
        private void OnGardenGrow() => HookCalled("OnGardenGrow");
        private void OnIngredientAdd() => HookCalled("OnIngredientAdd");
        private void OnItemAddedToDoor() => HookCalled("OnItemAddedToDoor");
        private void OnItemDrop() => HookCalled("OnItemDrop");
        private void OnItemHolderAddItem() => HookCalled("OnItemHolderAddItem");
        private void OnItemHolderTakeItem() => HookCalled("OnItemHolderTakeItem");
        private void OnPlankBreak() => HookCalled("OnPlantBreak");
        private void OnPlayerHitEnemy() => HookCalled("OnPlayerHitEnemy");
        private void OnRabbitAdd() => HookCalled("OnRabbitAdd");
        private void OnRabbitSpawn() => HookCalled("OnRabbitSpawn");
        private void OnRabbitTake() => HookCalled("OnRabbitTake");
        private void OnRackAdd() => HookCalled("OnRackAdd");
        private void OnRackRemove() => HookCalled("OnRackRemove");
        private void OnRaftControl() => HookCalled("OnRaftControl");
        private void OnRaftEnter() => HookCalled("OnRaftEnter");
        private void OnRaftExit() => HookCalled("OnRaftExit");
        private void OnRaftGrab() => HookCalled("OnRaftGrab");
        private void OnRaftPush() => HookCalled("OnRaftPush");
        private void OnRepairMaterialAdd() => HookCalled("OnRepairMaterialAdd");
        private void OnSledGrab() => HookCalled("OnSledGrab");
        private void OnStructureDestroy() => HookCalled("OnStructureDestroy");
        private void OnTrapReset() => HookCalled("OnTrapReset");
        private void OnTrapTrigger() => HookCalled("OnTrapTrigger");
        private void OnTreeDamage() => HookCalled("OnTreeDamage");
        private void OnWallAdditionToggle() => HookCalled("OnWallAdditionToggle");
        private void OnWallChunkPlace() => HookCalled("OnWallChunkPlace");
        private void OnWaterRemove() => HookCalled("OnWaterRemove");
    }
}
