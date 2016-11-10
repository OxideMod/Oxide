using UnityEngine;
using Oxide.Core;

namespace Oxide.Game.RustLegacy
{
    public class OnServerInitHook : MonoBehaviour
    {
        public void OnDestroy()
        {
            Interface.Call("OnServerInitialized", null);
        }
    }
}
