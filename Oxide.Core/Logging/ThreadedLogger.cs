using System.Threading;

namespace Oxide.Core.Logging
{
    /// <summary>
    /// Represents a logger that processes messages on a worker thread
    /// </summary>
    public abstract class ThreadedLogger : Logger
    {
        // Sync mechanisms
        private AutoResetEvent waitevent;
        private bool exit;
        private object syncroot;

        // The worker thread
        private Thread workerthread;

        /// <summary>
        /// Initializes a new instance of the ThreadedLogger class
        /// </summary>
        public ThreadedLogger()
            : base(false)
        {
            // Initialize
            waitevent = new AutoResetEvent(false);
            exit = false;
            syncroot = new object();

            // Create the thread
            workerthread = new Thread(Worker);
            workerthread.IsBackground = true;
            workerthread.Start();
        }

        ~ThreadedLogger()
        {
            exit = true;
            waitevent.Set();
            workerthread.Join();
        }

        /// <summary>
        /// Writes a message to the current logfile
        /// </summary>
        /// <param name="msg"></param>
        internal override void Write(LogMessage msg)
        {
            lock (syncroot)
                base.Write(msg);
            waitevent.Set();
        }

        /// <summary>
        /// Begins a batch process operation
        /// </summary>
        protected abstract void BeginBatchProcess();

        /// <summary>
        /// Finishes a batch process operation
        /// </summary>
        protected abstract void FinishBatchProcess();

        /// <summary>
        /// The worker thread
        /// </summary>
        private void Worker()
        {
            // Loop until it's time to exit
            while (!exit)
            {
                // Wait for signal
                waitevent.WaitOne();

                // Iterate each item in the queue
                lock (syncroot)
                {
                    if (messagequeue.Count > 0)
                    {
                        BeginBatchProcess();
                        try
                        {
                            while (messagequeue.Count > 0)
                            {
                                // Dequeue
                                LogMessage message = messagequeue.Dequeue();

                                // Process
                                ProcessMessage(message);
                            }
                        }
                        finally
                        {
                            FinishBatchProcess();
                        }
                    }
                }
            }
        }
    }
}
