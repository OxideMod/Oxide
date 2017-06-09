﻿using System;
using System.Reflection;
using Oxide.Core.Extensions;
using Oxide.Core.Unity.Plugins;
using UnityEngine;

namespace Oxide.Core.Unity
{
    /// <summary>
    /// The extension class that represents this extension
    /// </summary>
    public class UnityExtension : Extension
    {
        internal static readonly Version AssemblyVersion = Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Gets the name of this extension
        /// </summary>
        public override string Name => "Unity";

        /// <summary>
        /// Gets the version of this extension
        /// </summary>
        public override VersionNumber Version => new VersionNumber(AssemblyVersion.Major, AssemblyVersion.Minor, AssemblyVersion.Build);

        /// <summary>
        /// Gets the author of this extension
        /// </summary>
        public override string Author => "Oxide Team";

        /// <summary>
        /// Initializes a new instance of the UnityExtension class
        /// </summary>
        /// <param name="manager"></param>
        public UnityExtension(ExtensionManager manager) : base(manager)
        {
        }

        /// <summary>
        /// Loads this extension
        /// </summary>
        public override void Load()
        {
            Manager.RegisterPluginLoader(new UnityPluginLoader());

            Interface.Oxide.RegisterEngineClock(() => Time.realtimeSinceStartup);

            // Register our MonoBehaviour
            UnityScript.Create();
        }

        /// <summary>
        /// Loads plugin watchers used by this extension
        /// </summary>
        /// <param name="pluginDirectory"></param>
        public override void LoadPluginWatchers(string pluginDirectory)
        {
        }

        /// <summary>
        /// Called when all other extensions have been loaded
        /// </summary>
        public override void OnModLoad()
        {
        }
    }
}
