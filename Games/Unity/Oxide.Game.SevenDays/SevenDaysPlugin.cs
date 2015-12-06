using Oxide.Core;

using Oxide.Game.SevenDays.Libraries;

namespace Oxide.Plugins
{
    public abstract class SevenDaysPlugin : CSharpPlugin
    {
        protected SevenDays sdtd;

        public override void SetPluginInfo(string name, string path)
        {
            base.SetPluginInfo(name, path);

            sdtd = Interface.Oxide.GetLibrary<SevenDays>("SDTD");
        }

        protected void PrintToChat(string message) => PrintToChat("", message);

        protected void PrintToChat(string name, string message) => GameManager.Instance.GameMessageServer(null, message, name);
    }
}
