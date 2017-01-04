using Oxide.Core;
using Oxide.Game.SevenDays.Libraries;

namespace Oxide.Plugins
{
    public abstract class SevenDaysPlugin : CSharpPlugin
    {
        protected SevenDays sdtd = Interface.Oxide.GetLibrary<SevenDays>("SDTD");

        protected void PrintToChat(string message) => PrintToChat("", message);

        protected void PrintToChat(string name, string message) => GameManager.Instance.GameMessageServer(null, EnumGameMessages.Chat, message, name, false, null, false);
    }
}
