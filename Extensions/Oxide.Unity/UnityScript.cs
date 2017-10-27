using System;
using System.Reflection;
using UnityEngine;

namespace Oxide.Core.Unity
{
    /// <summary>
    /// The main MonoBehaviour which calls OxideMod.OnFrame
    /// </summary>
    public class UnityScript : MonoBehaviour
    {
        public static GameObject Instance { get; private set; }

        public static void Create()
        {
            Instance = new GameObject("Oxide.Core.Unity");
            DontDestroyOnLoad(Instance);
            Instance.AddComponent<UnityScript>();
        }

        private OxideMod oxideMod;

        private void Awake()
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

        private void Update() => oxideMod.OnFrame(Time.deltaTime);

        private void OnDestroy()
        {
            if (oxideMod.IsShuttingDown) return;
            oxideMod.LogWarning("The Oxide Unity Script was destroyed (creating a new instance)");
            oxideMod.NextTick(Create);
        }

        private void OnApplicationQuit()
        {
            if (!oxideMod.IsShuttingDown)
            {
                Interface.Call("OnServerShutdown");
                Interface.Oxide.OnShutdown();
            }
        }

        private void LogMessageReceived(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Exception) RemoteLogger.Exception(message, stackTrace);
        }
    }
}
