using System;
using System.IO;
using System.Linq;

using Oxide.Core;
using Oxide.Core.Extensions;

using UnityEngine;

namespace Oxide.Game.TheForest
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class TheForestExtension : Extension
    {
        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "TheForest";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(1, 0, 0);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        public override string[] WhitelistAssemblies => new[] { "Assembly-CSharp", "mscorlib", "Oxide.Core", "System", "System.Core", "UnityEngine" };
        public override string[] WhitelistNamespaces => new[] { "Steamworks", "System.Collections", "System.Security.Cryptography", "System.Text", "TheForest", "UnityEngine" };

        private static readonly string[] Filter =
        {
            "****** Game Activation Sequence ******",
            "Body Variation",
            "CanResume:",
            "DestroyPickup:",
            "Game Activation Sequence step",
            "HDR RenderTexture format is not supported on this platform",
            "Hull (UnityEngine.GameObject)",
            "Image Effects are not supported on this platform",
            "Joystick count=",
            "LobbyCreated param.m_eResult=k_EResult",
            "Refreshing Input Mapping Icons",
            "Reloading Input Mapping",
            "Skin Variation",
            "Skipped frame because",
            "Skipped rendering frame because",
            "The referenced script on this Behaviour is missing!",
            "WakeFromKnockOut",
            "[AmplifyMotion] Initialization failed",
            "attach: [",
            "delaying initial",
            "disableFlying",
            "enabled part 2",
            "going black",
            "null texture passed to GUI.DrawTexture",
            "planeCrash started",
            "setFemale",
            "setMale",
            "started steam server"
        };

        private static readonly string[] LogFilter =
        {
            "BMGlyph",
            "BMSymbol",
            "BetterList",
            "Image Effects are not supported on this platform",
            "TweenAlpha",
            "TweenColor",
            "TweenScale",
            "TweenTransform",
            "UIBasicSprite",
            "UIButton",
            "UICamera",
            "UIDragScrollView",
            "UIDrawCall",
            "UIGeometry",
            "UIGrid",
            "UIInput",
            "UIKeyNavigation",
            "UILabel",
            "UIPanel",
            "UIPlaySound",
            "UIPlayTween",
            "UIPopupList",
            "UIProgressBar",
            "UIRect",
            "UIRoot",
            "UIScrollBar",
            "UIScrollView",
            "UISlider",
            "UISprite",
            "UITexture",
            "UIToggle",
            "UITweener",
            "UIWidget",
            "UIWidget",
            "[AmplifyMotion] Initialization failed"
        };

        private const string LogFileName = "output_log.txt";
        private TextWriter _logWriter;

        /// <summary>
        /// Initializes a new instance of the TheForestExtension class
        /// </summary>
        /// <param name="manager"></param>
        public TheForestExtension(ExtensionManager manager)
            : base(manager)
        {

        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            // Register our loader
            Manager.RegisterPluginLoader(new TheForestPluginLoader());

            // Register our libraries
            Manager.RegisterLibrary("Forest", new Libraries.TheForest());
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="plugindir"></param>
        public override void LoadPluginWatchers(string plugindir)
        {

        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
            if (!Interface.Oxide.EnableConsole()) return;

            // Disable client audio
            TheForestCore.DisableAudio();

            // Limit FPS to reduce cpu usage
            PlayerPrefs.SetInt("MaxFrameRate2", 60); //Application.targetFrameRate = 256;

            if (File.Exists(LogFileName)) File.Delete(LogFileName);
            var logStream = File.AppendText(LogFileName);
            logStream.AutoFlush = true;
            _logWriter = TextWriter.Synchronized(logStream);

            Application.logMessageReceivedThreaded += HandleLog;

            Interface.Oxide.ServerConsole.Input += ServerConsoleOnInput;

            Interface.Oxide.ServerConsole.Title = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var players = CoopLobby.Instance?.MemberCount - 1;
                var hostname = CoopLobby.Instance?.Info?.Name.Split("()".ToCharArray())[0];
                return string.Concat(players, " | ", hostname);
            };

            Interface.Oxide.ServerConsole.Status1Left = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var hostname = CoopLobby.Instance?.Info.Name.Split("()".ToCharArray())[0];
                return string.Concat(" ", hostname);
            };
            Interface.Oxide.ServerConsole.Status1Right = () =>
            {
                var fps = Mathf.RoundToInt(1f / Time.smoothDeltaTime);
                var seconds = TimeSpan.FromSeconds(Time.realtimeSinceStartup);
                var uptime = $"{seconds.TotalHours:00}h{seconds.Minutes:00}m{seconds.Seconds:00}s".TrimStart(' ', 'd', 'h', 'm', 's', '0');
                return string.Concat(fps, "fps, ", uptime);
            };

            Interface.Oxide.ServerConsole.Status2Left = () =>
            {
                if (CoopLobby.Instance == null) return string.Empty;
                var players = CoopLobby.Instance?.MemberCount - 1;
                var playerLimit = CoopLobby.Instance?.Info?.MemberLimit;
                return string.Concat(" ", players, "/", playerLimit, " players");
            };
            Interface.Oxide.ServerConsole.Status2Right = () =>
            {
                // TODO: Network in/out
                return "";
            };

            Interface.Oxide.ServerConsole.Status3Left = () =>
            {
                //var gameTime = TheForestAtmosphere.Instance?.TimeOfDay; // TODO: Fix NRE and format
                return string.Concat(" "/*, gameTime*/);
            };
            Interface.Oxide.ServerConsole.Status3Right = () =>
            {
                var gameVersion = "0.24b"; // TODO: Grab version/protocol
                var oxideVersion = OxideMod.Version.ToString();
                return string.Concat("Oxide ", oxideVersion, " for ", gameVersion);
            };
            Interface.Oxide.ServerConsole.Status3RightColor = ConsoleColor.Yellow;
        }

        public override void OnShutdown()
        {
            _logWriter?.Flush();
            _logWriter?.Close();
        }

        private void ServerConsoleOnInput(string input)
        {
            // TODO
            if (string.IsNullOrEmpty(input)) return;
            if (input.Equals("coop")) //CoopSteamManager: Initialize / CoopSteamServer: .cctor
            {
                var coop = UnityEngine.Object.FindObjectOfType<TitleScreen>();
                coop.OnCoOp();
                coop.OnMpHost();
                coop.OnNewGame();
                TheForestCore.DisableAudio();
            }
            if (input.Equals("host")) //CoopSteamNGUI: OpenScreen LobbySetup
            {
                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostLobbySetup();
                TheForestCore.DisableAudio();
            }
            if (input.Equals("start")) //CoopLobbyManager: LobbyEnter Steamworks.LobbyEnter_t //ready for clients...on player connected start?
            {
                var coop = UnityEngine.Object.FindObjectOfType<CoopSteamNGUI>();
                coop.OnHostStartGame();
                TheForestCore.DisableAudio();
            }
            if (input.Equals("audio"))
            {
                TheForestCore.DisableAudio();
            }
        }

        private void HandleLog(string message, string stackTrace, LogType type)
        {
            if (string.IsNullOrEmpty(message)/* || LogFilter.Any(message.StartsWith)*/) return;
            _logWriter.WriteLine(message);
            if (!string.IsNullOrEmpty(stackTrace)) _logWriter.WriteLine(stackTrace);
            if (Filter.Any(message.StartsWith)) return;

            var color = ConsoleColor.Gray;
            if (type == LogType.Warning)
                color = ConsoleColor.Yellow;
            else if (type == LogType.Error)
                color = ConsoleColor.Red;
            Interface.Oxide.ServerConsole.AddMessage(message, color);
        }
    }
}
