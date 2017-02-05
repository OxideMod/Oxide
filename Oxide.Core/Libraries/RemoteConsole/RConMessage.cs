using Newtonsoft.Json;
using System;

namespace Oxide.Core.Libraries.RemoteConsole
{
    /// <summary>
    /// Message Send Between Server and Client
    /// Converted to JSON Format on Transmission
    /// </summary>
    [Serializable]
    public class RConMessage
    {
        public string Message;

        public int Identifier;

        public string Type;

        public string Stacktrace;

        public static RConMessage CreateMessage(string Msg, int Ident = 0, string Type = "Generic", string Trace = "") => new RConMessage() { Message = Msg, Identifier = Ident, Type = Type, Stacktrace = Trace };

        internal string ToJSON() => JsonConvert.SerializeObject(this, Formatting.Indented);

        public static RConMessage GetMessage(string JSONString) => (RConMessage)JsonConvert.DeserializeObject<RConMessage>(JSONString) ?? null;
    }
}