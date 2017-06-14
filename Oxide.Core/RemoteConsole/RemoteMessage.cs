using Newtonsoft.Json;
using System;

namespace Oxide.Core.RemoteConsole
{
    /// <summary>
    /// Message sent between the server and all connected clients
    /// </summary>
    [Serializable]
    public class RemoteMessage
    {
        public string Message;
        public int Identifier;
        public string Type;
        public string Stacktrace;

        public static RemoteMessage CreateMessage(string message, int ident = 0, string type = "Generic", string trace = "") => new RemoteMessage()
        {
            Message = message,
            Identifier = ident,
            Type = type,
            Stacktrace = trace
        };

        public static RemoteMessage GetMessage(string message)
        {
            try
            {
                var msg = JsonConvert.DeserializeObject<RemoteMessage>(message);
                return msg;
            }
            catch(JsonReaderException)
            {
                Interface.Oxide.LogError("[Rcon] Failed to parse message. Incorrect format");
                return null;
            }
        }

        internal string ToJSON() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
