using Oxide.Core;

using UnityEngine;

namespace Oxide.Ext.RustLegacy
{
    public class OnServerInitHook : MonoBehaviour
    {
        public void OnDestroy()
        {
            Interface.Oxide.CallHook("OnServerInitialized", null);
        }
    }
}
