using System;

namespace Oxide.Core.Libraries.Covalence
{
    [Serializable]
    public class CommandAlreadyExistsException : Exception
    {
        public CommandAlreadyExistsException() { }
        public CommandAlreadyExistsException(string cmd) : base($"Command {cmd} already exists") { }
        public CommandAlreadyExistsException(string message, Exception inner) : base(message, inner) { }
        protected CommandAlreadyExistsException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context)
        { }
    }
}
