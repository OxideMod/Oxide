using Assets.Scripts.Core;
using Oxide.Core.Libraries;
using Oxide.Game.Hurtworld.Libraries.Covalence;

namespace Oxide.Game.Hurtworld.Libraries
{
    public class Item : Library
    {
        // Covalence references
        internal static readonly HurtworldCovalenceProvider Covalence = HurtworldCore.Covalence;

        // Game references
        internal static readonly GlobalItemManager ItemManager = GlobalItemManager.Instance;

        /// <summary>
        /// Gets item based on item ID
        /// </summary>
        /// <param name="itemId"></param>
        [LibraryFunction("GetItem")]
        public static IItem GetItem(int itemId) => ItemManager.GetItem(itemId);
    }
}
