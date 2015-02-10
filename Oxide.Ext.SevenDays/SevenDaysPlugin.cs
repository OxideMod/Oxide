namespace Oxide.Plugins
{
    public abstract class SevenDaysPlugin : CSharpPlugin
    {
        GameManager _gameManager;
        protected GameManager gameManager
        {
            get
            {
                if (_gameManager == null)
                    _gameManager = UnityEngine.Object.FindObjectOfType<GameManager>();
                return _gameManager;
            }
        }

        protected void PrintToChat(string message)
        {
            PrintToChat("", message);
        }

        protected void PrintToChat(string name, string message)
        {
            gameManager.SendChatMessage(message, -1, name);
        }
    }
}
