using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Core.ServerConsole
{
    public class ConsoleSystemEventArgs : EventArgs
    {
        public new static readonly EventArgs Empty;

        public readonly string Message;

        public readonly ConsoleColor Color;

        public ConsoleSystemEventArgs(string Message, ConsoleColor color)
        {
            this.Message = Message;
            Color = color;
        }
    }
}
