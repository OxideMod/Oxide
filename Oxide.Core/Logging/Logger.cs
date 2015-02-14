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
        protected Queue<LogMessage> messagequeue;

        // Should messages be processed immediately and on the same thread?
        private bool processmessagesimmediately;

        /// <summary>
        /// Initialises a new instance of the Logger class
        /// </summary>
        public Logger(bool processmessagesimmediately)
        {
            // Initialise
            this.processmessagesimmediately = processmessagesimmediately;
            if (!processmessagesimmediately)
                messagequeue = new Queue<LogMessage>();
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
            LogMessage msg = new LogMessage { Type = type, Message = string.Format("{0} [{1}] {2}", DateTime.Now.ToShortTimeString(), type, format) };
            if (args.Length != 0)
                msg.Message = string.Format(msg.Message, args);
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
            LogMessage msg = CreateLogMessage(type, format, args);

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
            if (processmessagesimmediately)
                ProcessMessage(msg);
            else
                messagequeue.Enqueue(msg);
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
                Write(LogType.Error, string.Format("{0}{1}{2}", message, Environment.NewLine, formatted));
                return;
            }
            while (ex.InnerException != null) ex = ex.InnerException;
            Write(LogType.Error, string.Format("{0} ({1}: {2})", message, ex.GetType().Name, ex.Message));
            Write(LogType.Debug, "{0}", ex.StackTrace);
        }
    }
}
