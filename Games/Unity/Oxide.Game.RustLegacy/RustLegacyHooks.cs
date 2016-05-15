using Oxide.Core;

using UnityEngine;

namespace Oxide.Game.RustLegacy
{
    public class OnServerInitHook : MonoBehaviour
    {
        public void OnDestroy()
        {
            Interface.CallHook("OnServerInitialized", null);
        }
    }
}
