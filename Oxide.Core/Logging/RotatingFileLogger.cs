using System;
using System.IO;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// A logger that writes to a set of files that rotate by day
    /// </summary>
    public sealed class RotatingFileLogger : ThreadedLogger
    {
        /// <summary>
        /// Gets the directory to write log files to
        /// </summary>
        public string Directory { get; set; }

        // The active writer
        private StreamWriter writer;

        /// <summary>
        /// Gets the filename for the specified date
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        private string GetLogFilename(DateTime date) => Path.Combine(Directory, $"oxide_{date:yyyy-MM-dd}.txt");

        /// <summary>
        /// Begins a batch process operation
        /// </summary>
        protected override void BeginBatchProcess()
        {
            // Open the writer
            writer = new StreamWriter(new FileStream(GetLogFilename(DateTime.Now), FileMode.Append, FileAccess.Write));
        }

        /// <summary>
        /// Processes the specified message
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessMessage(LogMessage message) => writer.WriteLine(message.Message);

        /// <summary>
        /// Finishes a batch process operation
        /// </summary>
        protected override void FinishBatchProcess()
        {
            // Close the writer
            writer.Close();
            writer.Dispose();
            writer = null;
        }
    }
}
