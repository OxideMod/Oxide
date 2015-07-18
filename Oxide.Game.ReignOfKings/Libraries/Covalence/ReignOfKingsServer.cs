using System.Linq;
using System.Net;

using CodeHatch.Engine.Core.Commands;
using CodeHatch.Engine.Networking;

using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    class ReignOfKingsServer : IServer
    {
        public string Name => DedicatedServerBypass.Settings.ServerName;
        public IPAddress Address
        {
            get
            {
                var data = CodeHatch.Engine.Core.Gaming.Game.ServerData;
                //or data.IPLocal ?!?
                if (string.IsNullOrEmpty(data.IP)) return null;
                return IPAddress.Parse(data.IP);
            }
        }

        public ushort Port => CodeHatch.Engine.Core.Gaming.Game.ServerData.Port;

        public void Print(string message)
        {
            UnityEngine.Debug.Log(message);
        }

        public void RunCommand(string command, params object[] args)
        {
            CommandManager.ExecuteCommand(Server.Instance.ServerPlayer.Id, command + " " + string.Join(" ", args.ToList().ConvertAll(a => (string)a).ToArray()));
        }
    }
}
