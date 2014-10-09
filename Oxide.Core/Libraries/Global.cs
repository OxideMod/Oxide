using System;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// A global library containing game-agnostic and language-agnostic utilities
    /// </summary>
    public class Global : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return true; } }

        /// <summary>
        /// Returns a version structure
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [LibraryFunction("V")]
        public VersionNumber MakeVersion(ushort major, ushort minor, ushort patch)
        {
            return new VersionNumber(major, minor, patch);
        }
    }
}
