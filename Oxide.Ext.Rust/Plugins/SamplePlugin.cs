// Reference: Oxide.Ext.Rust

namespace Oxide.Plugins
{
    [Info("Sample Plugin", "bawNg", 0.1)]
    class SamplePlugin : RustPlugin
    {
        void Loaded()
        {

        }

        void OnPlayerInit(BasePlayer new_player)
        {
            foreach (var player in BasePlayer.activePlayerList)
                PrintToChat(player, "{0} has connected", new_player.displayName);
        }

        [ConsoleCommand("sample")]
        void cmdConsoleSample(ConsoleSystem.Arg arg)
        {
            SendReply(arg, "SamplePlugin: You typed 'sample' in console");
        }

        [ChatCommand("sample")]
        void cmdChatSample(BasePlayer player, string command, string[] args)
        {
            SendReply(player, "SamplePlugin: You typed /sample in chat");
        }
    }
}
