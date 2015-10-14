namespace Oxide.Plugins
{
    public abstract class SevenDaysPlugin : CSharpPlugin
    {
        protected void PrintToChat(string message)
        {
            PrintToChat("", message);
        }

        protected void PrintToChat(string name, string message)
        {
            GameManager.Instance.GameMessageServer(null, message, name);
        }
    }
}
