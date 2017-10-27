extern alias Oxide;

using Oxide::Newtonsoft.Json;
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

        public static RemoteMessage CreateMessage(string message, int identifier = 0, string type = "generic", string trace = "") => new RemoteMessage()
        {
            Message = message,
            Identifier = identifier,
            Type = type.ToLower(),
            Stacktrace = trace
        };

        public static RemoteMessage GetMessage(string message)
        {
            try
            {
                return JsonConvert.DeserializeObject<RemoteMessage>(message);
            }
            catch (JsonReaderException)
            {
                Interface.Oxide.LogError("[Rcon] Failed to parse message, incorrect format");
                return null;
            }
        }

        internal string ToJSON() => JsonConvert.SerializeObject(this, Formatting.Indented);
    }
}
