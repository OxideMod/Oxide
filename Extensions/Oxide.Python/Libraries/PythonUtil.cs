using Oxide.Core.Libraries;
using System;

namespace Oxide.Core.Python.Libraries
{
    /// <summary>
    /// A utility library for Python specific functions
    /// </summary>
    public class PythonUtil : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal => false;

        /// <summary>
        /// Converts the specified object to the specified type and sets it on the array
        /// </summary>
        /// <param name="array"></param>
        /// <param name="index"></param>
        /// <param name="value"></param>
        /// <param name="type"></param>
        [LibraryFunction("ConvertAndSetOnArray")]
        public bool ConvertAndSetOnArray(object[] array, int index, object value, Type type)
        {
            object converted;
            try
            {
                converted = Convert.ChangeType(value, type);
            }
            catch (Exception)
            {
                return false;
            }
            array[index] = converted;
            return true;
        }
    }
}
