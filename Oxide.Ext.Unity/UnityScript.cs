using System.Reflection;
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
            
            if (typeof(Application).GetEvent("logMessageReceived") == null)
            {
                // Unity 4   
                var log_callback_field = typeof(Application).GetField("s_LogCallback", BindingFlags.Static | BindingFlags.NonPublic);
                var log_callback = log_callback_field.GetValue(null) as Application.LogCallback;
                if (log_callback == null)
                    Interface.Oxide.LogWarning("No Unity application log callback is registered");
                else
                    Application.RegisterLogCallback((message, stack_trace, type) =>
                    {
                        log_callback.Invoke(message, stack_trace, type);
                        LogMessageReceived(message, stack_trace, type);
                    });
            }
            else
            {
                // Unity 5
                Application.logMessageReceived += LogMessageReceived;
            }
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

        void LogMessageReceived(string message, string stack_trace, LogType type)
        {
            if (type == LogType.Exception && stack_trace.Contains("Oxide"))
                RemoteLogger.Exception(message, stack_trace);
        }
    }
}