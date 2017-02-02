using Oxide.Core.Libraries;
using Oxide.Core.Libraries.Covalence;
using Oxide.Game.Hurtworld.Libraries.Covalence;
using UnityEngine;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Server : Library
    {
        // Covalence references
        internal static readonly HurtworldCovalenceProvider Covalence = HurtworldCore.Covalence;
        internal static readonly IServer ServerInstance = Covalence.CreateServer();

        #region Chat and Commands

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        public void Broadcast(string message, string prefix = null) => ServerInstance.Broadcast(prefix != null ? $"{prefix} {message}" : message);

        /// <summary>
        /// Broadcasts a chat message to all players
        /// </summary>
        /// <param name="message"></param>
        /// <param name="prefix"></param>
        /// <param name="args"></param>
        public void Broadcast(string message, string prefix = null, params object[] args) => Broadcast(string.Format(message, args), prefix);

        /// <summary>
        /// Runs the specified server command
        /// </summary>
        /// <param name="command"></param>
        /// <param name="args"></param>
        public void Command(string command, params object[] args) => ServerInstance.Command(command, args);

        #endregion

        #region Object Control

        public void DestroyObject(GameObject obj) => HNetworkManager.Instance.NetDestroy(obj.uLinkNetworkView());

        public void MoveObject(GameObject obj, Vector3 destination) => obj.GetComponent<Transform>().position = destination;

        public GameObject SpawnObject(string obj, Vector3 position, Quaternion angle)
        {
            return HNetworkManager.Instance.NetInstantiate(uLink.NetworkPlayer.server, obj, position, angle, GameManager.GetSceneTime());
        }

        public GameObject ObjectByName(string partialName)
        {
            var gos = Object.FindObjectsOfType<GameObject>();
            foreach (var g in gos) if (g.name.Contains(partialName)) return g;
            return null;
        }

        public void AttachComponent(string objectName, Component component)
        {
            var gos = Object.FindObjectsOfType<GameObject>();
            foreach (var g in gos)
            {
                if (!g.activeInHierarchy) continue;
                if (g.name.Contains(objectName)) g.AddComponent(component.GetType());
            }
        }

        #endregion
    }
}
