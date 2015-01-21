using System;

using Oxide.Core.Plugins;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// The time library
    /// </summary>
    public class Time : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }

        private static readonly DateTime epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Returns a Unix timestamp for the current time
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetUnixTimestamp")]
        public uint GetUnixTimestamp()
        {
            return (uint)DateTime.UtcNow.Subtract(epoch).TotalSeconds;
        }

        /// <summary>
        /// Returns DateTime.UtcNow
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetCurrentTime")]
        public DateTime GetCurrentTime()
        {
            return DateTime.UtcNow;
        }
    }
}
