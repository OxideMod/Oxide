using System;
using System.Reflection;

using UnityEngine;

using Oxide.Core;

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
            oxideMod = Interface.Oxide;

            var eventInfo = typeof(Application).GetEvent("logMessageReceived");
            if (eventInfo == null)
            {
                // Unity 4
                var logCallbackField = typeof(Application).GetField("s_LogCallback", BindingFlags.Static | BindingFlags.NonPublic);
                var logCallback = logCallbackField?.GetValue(null) as Application.LogCallback;
                if (logCallback == null) Interface.Oxide.LogWarning("No Unity application log callback is registered");

                #pragma warning disable 0618
                Application.RegisterLogCallback((message, stack_trace, type) =>
                {
                    logCallback?.Invoke(message, stack_trace, type);
                    LogMessageReceived(message, stack_trace, type);
                });
            }
            else
            {
                // Unity 5
                var handleException = Delegate.CreateDelegate(eventInfo.EventHandlerType, this, "LogMessageReceived");
                eventInfo.GetAddMethod().Invoke(null, new object[] { handleException });
            }
        }

        void Update() => oxideMod.OnFrame(Time.deltaTime);

        void OnDestroy()
        {
            if (oxideMod.IsShuttingDown) return;
            oxideMod.LogWarning("The Oxide Unity Script was destroyed (creating a new instance)");
            oxideMod.NextTick(Create);
        }

        void OnApplicationQuit()
        {
            if (!oxideMod.IsShuttingDown)
            {
                Interface.Oxide.CallHook("OnServerShutdown");
                Interface.Oxide.OnShutdown();
            }
        }

        void LogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception && stackTrace.Contains("Oxide")) RemoteLogger.Exception(message, stackTrace);
        }
    }
}
