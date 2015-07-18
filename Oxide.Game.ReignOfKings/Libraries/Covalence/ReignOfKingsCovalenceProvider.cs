using Oxide.Core.Libraries.Covalence;

namespace Oxide.Game.ReignOfKings.Libraries.Covalence
{
    class ReignOfKingsCovalenceProvider : ICovalenceProvider
    {
        public string GameName => "ReignOfKings";

        /// <summary>
        /// Gets the singleton instance of this provider
        /// </summary>
        internal static ReignOfKingsCovalenceProvider Instance { get; private set; }

        /// <summary>
        /// Gets the player manager
        /// </summary>
        public ReignOfKingsPlayerManager PlayerManager { get; private set; }

        /// <summary>
        /// Gets the command system provider
        /// </summary>
        public ReignOfKingsCommandSystem CommandSystem { get; private set; }

        public ReignOfKingsCovalenceProvider()
        {
            Instance = this;
        }

        public IServer CreateServer()
        {
            return new ReignOfKingsServer();
        }

        public IPlayerManager CreatePlayerManager()
        {
            return PlayerManager = new ReignOfKingsPlayerManager();
        }

        public ICommandSystem CreateCommandSystemProvider()
        {
            return CommandSystem = new ReignOfKingsCommandSystem();
        }
    }
}
