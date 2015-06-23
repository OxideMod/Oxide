using System;

namespace Oxide.Core.Libraries.Covalence
{
    /// <summary>
    /// Represents a position of a point in 3D space
    /// </summary>
    public struct GenericPosition
    {
        public readonly float X, Y, Z;

        public GenericPosition(float x, float y, float z)
        {
            X = x; Y = y; Z = z;
        }
    }

    /// <summary>
    /// Represents a generic character controlled by a player
    /// </summary>
    public interface IPlayerCharacter
    {
        /// <summary>
        /// Gets the owner of this character
        /// </summary>
        ILivePlayer Owner { get; }

        /// <summary>
        /// Gets the object that backs this character, if available
        /// </summary>
        object Object { get; }

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void GetPosition(out float x, out float y, out float z);

        /// <summary>
        /// Gets the position of this character
        /// </summary>
        /// <returns></returns>
        GenericPosition GetPosition();

        #region Manipulation

        /// <summary>
        /// Causes this character to die
        /// </summary>
        void Kill();

        /// <summary>
        /// Teleports this character to the specified position
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        void Teleport(float x, float y, float z);

        #endregion

    }
}
