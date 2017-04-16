using System;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Seven Days 2 Die Sample", "bawNg", 0.1)]
    class SamplePlugin : CSharpPlugin
    {
        void Loaded()
        {
            Puts("SamplePlugin: Basic Seven Days 2 Die support has been loaded.");
        }

        void Unloaded()
        {
            Puts("SamplePlugin: Unloaded");
        }

        void OnServerInitialized()
        {
            Puts("Server has been initialized!");
        }

        void OnPlayerConnected(ClientInfo client)
        {
            Puts(client.playerName + " has connected!");
            Type.GetType("5E2AA").GetField("gameManager").SendChatMessage(client.playerName + " has connected!", -1, "");
        }

        void OnPlayerDisconnected(ClientInfo client)
        {
            Puts(client.playerName + " has disconnected!");
            Type.GetType("5E2AA").GetField("gameManager").SendChatMessage(client.playerName + " has disconnected!", -1, "");
        }

        void OnEntityDeath()
        {
            Puts("OnEntityDeath is working!");
        }
    }
}