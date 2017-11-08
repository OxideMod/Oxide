using System;

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
        public override bool IsGlobal => false;

        private static readonly DateTime Epoch = new DateTime(1970, 1, 1);

        /// <summary>
        /// Returns a Unix timestamp for the current time
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetUnixTimestamp")]
        public uint GetUnixTimestamp() => (uint)DateTime.UtcNow.Subtract(Epoch).TotalSeconds;

        /// <summary>
        /// Returns a Unix timestamp for the specified time
        /// </summary>
        /// <param name="time">DateTime</param>
        /// <returns></returns>
        [LibraryFunction("GetUnixTimestamp")]
        public uint GetUnixTimestamp(DateTime time) => (uint)time.Subtract(Epoch).TotalSeconds;

        /// <summary>
        /// Returns a DateTime from a unix timestamp
        /// </summary>
        /// <param name="timestamp">Unix timestamp</param>
        /// <returns></returns>
        [LibraryFunction("GetDateTimeFromUnix")]
        public DateTime GetDateTimeFromUnix(uint timestamp) => Epoch.AddSeconds(timestamp);

        /// <summary>
        /// Returns DateTime.UtcNow
        /// </summary>
        /// <returns></returns>
        [LibraryFunction("GetCurrentTime")]
        public DateTime GetCurrentTime() => DateTime.UtcNow;
    }
}
