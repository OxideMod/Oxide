namespace Oxide.Core.Logging
{
    public class CallbackLogger : Logger
    {
        private NativeDebugCallback callback;

        /// <summary>
        /// Initialises a new instance of the CallbackLogger class
        /// </summary>
        /// <param name="callback"></param>
        public CallbackLogger(NativeDebugCallback callback)
            : base(true)
        {
            this.callback = callback;
        }

        /// <summary>
        /// Processes the specified message
        /// </summary>
        /// <param name="message"></param>
        protected override void ProcessMessage(LogMessage message) => callback?.Invoke(message.Message);
    }
}
