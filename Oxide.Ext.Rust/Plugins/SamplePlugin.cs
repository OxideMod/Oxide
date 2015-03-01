// Reference: Oxide.Ext.Rust

using Oxide.Core.Plugins;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("CSharp 6 Sample", "bawNg", 0.1)]
    public class SamplePlugin : RustPlugin
    {
        // Define a shortcut property to get the amount of seconds since the server started
        float Now => Time.realtimeSinceStartup;

        // Create a dynamic plugin reference which will be the "push" plugin when it is loaded
        [PluginReference] Plugin push;

        // Initialize indexes using the new index initialization syntax
        Hash<string, string> weaponAliases = new Hash<string, string>
        {
            ["ak47"] = "rifle_ak",
            ["bolt"] = "rifle_bolt"
        };

        string helpText = "Help goes here";

        void Loaded()
        {
            // Use string interpolation to format a float with 3 decimal points instead of calling string.Format()
            Puts($"Plugin loaded after {Now:0.000} seconds");
        }

        void OnPlayerInit(Network.Connection connection)
        {
            var player = connection.player as BasePlayer;
            if (!player) return;
            // Call a method which has an optional argument
            SendHelpToPlayer(player);
            // Call a method using a named argument for one of the optional arguments
            ItemManager.CreateByItemID(ItemManager.FindItemDefinition("rock").itemid, isBlueprint: true);
            // Send a push notification if the "push" plugin is currently loaded
            push?.Call("PushMessage", "Player connected", $"{player.displayName} connected", "low");
        }

        [ChatCommand("address")]
        void AddressCommand(BasePlayer player, string command, string[] args)
        {
            if (args.Length < 1)
            {
                SendReply(player, "You need to enter a players name");
                return;
            }
            var target = BasePlayer.Find(args[0]);
            if (target)
            {
                var address = target?.net?.connection?.ipaddress ?? "unknown";
                SendReply(player, $"{target.displayName}'s address is {address}");
            }
            else
                SendReply(player, $"Player not found: {args[0]}");
        }

        [ConsoleCommand("give.me")]
        void GiveMeCommand(ConsoleSystem.Arg arg)
        {
            var player = arg.connection.player as BasePlayer;
            if (!player || arg.Args.Length < 1) return;
            var name = arg.Args[0];
            var item = ItemManager.CreateByName(weaponAliases[name] ?? name);
            if (item != null) player.GiveItem(item);
        }

        // Define a method with optional arguments, instead of having to use multiple overloaded methods
        void SendHelpToPlayer(BasePlayer player, bool already_welcomed = false)
        {
            player.ChatMessage(already_welcomed ? helpText : $"Welcome {player.displayName}!");
        }
    }
}