namespace Oxide.Plugins
{
    [Info("Sample Plugin", "bawNg", 0.1)]
    class SamplePlugin : CSharpPlugin
    {
        void Loaded()
        {
            Puts("Hello from SamplePlugin");
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