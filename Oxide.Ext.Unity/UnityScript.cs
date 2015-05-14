using System;
using System.Reflection;

using Oxide.Core;

using UnityEngine;

namespace Oxide.Ext.Unity
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
            DontDestroyOnLoad(Instance);
            Instance.AddComponent<UnityScript>();
        }

        private OxideMod oxideMod;

        void Awake()
        {
            oxideMod = Interface.GetMod();

            var event_info = typeof(Application).GetEvent("logMessageReceived");
            if (event_info == null)
            {
                // Unity 4
                var log_callback_field = typeof(Application).GetField("s_LogCallback", BindingFlags.Static | BindingFlags.NonPublic);
                var log_callback = log_callback_field?.GetValue(null) as Application.LogCallback;
                if (log_callback == null) Interface.Oxide.LogWarning("No Unity application log callback is registered");
                Application.RegisterLogCallback((message, stack_trace, type) =>
                {
                    log_callback?.Invoke(message, stack_trace, type);
                    LogMessageReceived(message, stack_trace, type);
                });
            }
            else
            {
                // Unity 5
                var handle_exception = Delegate.CreateDelegate(event_info.EventHandlerType, this, "LogMessageReceived");
                event_info.GetAddMethod().Invoke(null, new object[] { handle_exception });
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
