using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;

namespace Oxide.Game.RustLegacy
{
    /// <summary>
    /// Game hooks and wrappers for the core Rust Legacy plugin
    /// </summary>
    public partial class RustLegacyCore : CSPlugin
    {
        #region Player Hooks

        #endregion
    }

    public class OnServerInitHook : MonoBehaviour
    {
        public void OnDestroy() => Interface.Call("OnServerInitialized", null);
    }
}
