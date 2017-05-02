using System;

namespace Oxide.Core.Logging
{
    public class LogMessageEventArgs : EventArgs
    {
        public new static readonly EventArgs Empty;

        public readonly string Message;

        public readonly string MessageType;

        public LogMessageEventArgs(string Message, LogType Type)
        {
            this.Message = Message;
            MessageType = Type.ToString();        }
    }
}
