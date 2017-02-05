﻿using Assets.Scripts.Core;
using Oxide.Core.Libraries;
using UnityEngine;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Item : Library
    {
        // Game references
        internal static readonly GlobalItemManager ItemManager = GlobalItemManager.Instance;

        /// <summary>
        /// Gets item based on item ID
        /// </summary>
        /// <param name="itemId"></param>
        public static IItem GetItem(int itemId) => ItemManager.GetItem(itemId);

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
