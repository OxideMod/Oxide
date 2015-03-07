using Oxide.Core;

using UnityEngine;

namespace Oxide.Unity
{
    /// <summary>
    /// The main MonoBehaviour which calls OxideMod.OnFrame
    /// </summary>
    public class UnityScript : MonoBehaviour
    {
        private OxideMod oxideMod;

        void Awake()
        {
            oxideMod = Interface.GetMod();
        }

        void Update()
        {
            oxideMod.OnFrame();
        }
    }
}