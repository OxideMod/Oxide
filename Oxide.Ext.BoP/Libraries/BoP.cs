using Oxide.Core.Libraries;

namespace Oxide.BoP.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for The Forest
    /// </summary>
    public class BoP : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }
    }
}
