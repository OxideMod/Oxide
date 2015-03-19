using Oxide.Core;

using UnityEngine;

namespace Oxide.Unity
{
    /// <summary>
    /// The main MonoBehaviour which calls OxideMod.OnFrame
    /// </summary>
    public class UnityScript : MonoBehaviour
    {
        public static GameObject Instance { get; private set; }

        public static void Create()
        {
            Instance = new GameObject("Oxide.Ext.Unity");
            Object.DontDestroyOnLoad(Instance);
            Instance.AddComponent<UnityScript>();
        }

        private OxideMod oxideMod;

        void Awake()
        {
            oxideMod = Interface.GetMod();
        }

        void Update()
        {
            oxideMod.OnFrame();
        }

        void OnDestroy()
        {
            oxideMod.RootLogger.Write(Core.Logging.LogType.Warning, "The Oxide Unity Script was destroyed (creating a new instance)");
            oxideMod.NextTick(Create);
        }
    }
}