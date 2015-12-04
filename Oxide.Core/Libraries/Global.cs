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
        public override bool IsGlobal => true;

        /// <summary>
        /// Returns a version structure
        /// </summary>
        /// <param name="major"></param>
        /// <param name="minor"></param>
        /// <param name="patch"></param>
        /// <returns></returns>
        [LibraryFunction("V")]
        public VersionNumber MakeVersion(ushort major, ushort minor, ushort patch) => new VersionNumber(major, minor, patch);

        /// <summary>
        /// Creates a new instance of the specified type
        /// </summary>
        /// <param name="type"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        [LibraryFunction("new")]
        public object New(Type type, object[] args) => args == null ? Activator.CreateInstance(type) : Activator.CreateInstance(type, args);
    }
}
