using Oxide.Core.Libraries;

namespace Oxide.ReignOfKings.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for Reign of Kings
    /// </summary>
    public class ReignOfKings : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }
    }
}
