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
            Application.logMessageReceived += HandleException;
        }

        void Update()
        {
            oxideMod.OnFrame(Time.deltaTime);
        }

        void OnDestroy()
        {
            if (oxideMod.IsShuttingDown) return;
            oxideMod.LogWarning("The Oxide Unity Script was destroyed (creating a new instance)");
            oxideMod.NextTick(Create);
        }

        void HandleException(string message, string stack_trace, LogType type)
        {
            if (type == LogType.Exception && stack_trace.Contains("Oxide"))
                RemoteLogger.Exception(message, stack_trace);
        }
    }
}