using System;
using System.Collections.Generic;

namespace Oxide.Core.Logging
{
    public enum LogType { Info, Debug, Warning, Error }

    /// <summary>
    /// Represents a logger
    /// </summary>
    public abstract class Logger
    {
        /// <summary>
        /// Represents a single log message
        /// </summary>
        public struct LogMessage
        {
            public LogType Type;
            public string Message;
        }

        // The message queue
        protected Queue<LogMessage> MessageQueue;

        // Should messages be processed immediately and on the same thread?
        private bool processImediately;

        /// <summary>
        /// Initializes a new instance of the Logger class
        /// </summary>
        /// <param name="processImediately"></param>
        protected Logger(bool processImediately)
        {
            // Initialize
            this.processImediately = processImediately;
            if (!processImediately) MessageQueue = new Queue<LogMessage>();
        }

        /// <summary>
        /// Creates a log message from the specified arguments
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        protected LogMessage CreateLogMessage(LogType type, string format, object[] args)
        {
            var msg = new LogMessage { Type = type, Message = format };
            if (args.Length != 0) msg.Message = string.Format(msg.Message, args);
            return msg;
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="type"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public virtual void Write(LogType type, string format, params object[] args)
        {
            // Create the structure
            var msg = CreateLogMessage(type, format, args);

            // Pass to overload
            Write(msg);
        }

        /// <summary>
        /// Writes a message to this logger
        /// </summary>
        /// <param name="msg"></param>
        internal virtual void Write(LogMessage msg)
        {
            // If we're set to process immediately, do so, otherwise enqueue
            if (processImediately)
                ProcessMessage(msg);
            else
                MessageQueue.Enqueue(msg);
        }

        /// <summary>
        /// Processes the specified message
        /// </summary>
        /// <param name="message"></param>
        protected virtual void ProcessMessage(LogMessage message)
        {
        }

        /// <summary>
        /// Writes an exception to this logger
        /// </summary>
        /// <param name="message"></param>
        /// <param name="ex"></param>
        public virtual void WriteException(string message, Exception ex)
        {
            var formatted = ExceptionHandler.FormatException(ex);
            if (formatted != null)
            {
                Write(LogType.Error, $"{message}{Environment.NewLine}{formatted}");
                return;
            }
            var outerEx = ex;
            while (ex.InnerException != null) ex = ex.InnerException;
            if (outerEx.GetType() != ex.GetType()) Write(LogType.Debug, "ExType: {0}", outerEx.GetType().Name);
            Write(LogType.Error, $"{message} ({ex.GetType().Name}: {ex.Message})");
            Write(LogType.Debug, "{0}", ex.StackTrace);
        }

        /// <summary>
        /// Called when logger is removed
        /// </summary>
        public virtual void OnRemoved()
        {
        }
    }
}
