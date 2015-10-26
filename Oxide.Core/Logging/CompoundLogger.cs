using System.Collections.Generic;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// Represents a set of loggers that fall under a single logger
    /// </summary>
    public sealed class CompoundLogger : Logger
    {
        // Loggers under this compound logger
        private readonly HashSet<Logger> subloggers;

        // Any cached messages for new loggers
        private readonly List<LogMessage> messagecache;
        private bool usecache;

        /// <summary>
        /// Initializes a new instance of the CompoundLogger class
        /// </summary>
        public CompoundLogger()
            : base(true)
        {
            // Initialize
            subloggers = new HashSet<Logger>();
            messagecache = new List<LogMessage>();
            usecache = true;
        }

        /// <summary>
        /// Adds a sublogger to this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void AddLogger(Logger logger)
        {
            // Register it
            subloggers.Add(logger);

            // Write the message cache to it
            foreach (var t in messagecache) logger.Write(t);
        }

        /// <summary>
        /// Removes a sublogger from this compound logger
        /// </summary>
        /// <param name="logger"></param>
        public void RemoveLogger(Logger logger)
        {
            // Unregister it
            subloggers.Remove(logger);
        }

        /// <summary>
        /// Writes a message to all subloggers of this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public override void Write(LogType type, string format, params object[] args)
        {
            // Write to all current subloggers
            foreach (var logger in subloggers) logger.Write(type, format, args);

            // Cache it for any loggers added late
            if (usecache) messagecache.Add(CreateLogMessage(type, format, args));
        }

        /// <summary>
        /// Disables logger message cache
        /// </summary>
        public void DisableCache()
        {
            usecache = false;
            messagecache.Clear();
        }
    }
}
