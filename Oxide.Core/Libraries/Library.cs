using System;
using System.Reflection;
using System.Linq;
using System.Collections.Generic;

namespace Oxide.Core.Libraries
{
    /// <summary>
    /// Indicates that the specified function is a library function with a name
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class LibraryFunction : Attribute
    {
        /// <summary>
        /// Gets the name for the library function
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Initialises a new instance of the LibraryFunction class
        /// </summary>
        /// <param name="name"></param>
        public LibraryFunction(string name)
        {
            Name = name;
        }
    }

    /// <summary>
    /// Represents a library containing a set of functions for script languages to use
    /// </summary>
    public abstract class Library
    {
        // Functions stored in this library
        private IDictionary<string, MethodInfo> functions;

        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public abstract bool IsGlobal { get; }

        /// <summary>
        /// Initialises a new instance of the Library class
        /// </summary>
        public Library()
        {
            // Load all functions
            functions = new Dictionary<string, MethodInfo>();
            foreach (MethodInfo method in GetType().GetMethods())
            {
                LibraryFunction func;
                try
                {
                    func = method.GetCustomAttributes(typeof(LibraryFunction), true).Single() as LibraryFunction;
                }
                catch (Exception)
                {
                    func = null;
                }
                if (func != null)
                {
                    functions.Add(func.Name, method);
                }
            }
        }

        /// <summary>
        /// Gets all function names in this library
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFunctionNames()
        {
            return functions.Keys;
        }

        /// <summary>
        /// Gets a function by the specified name
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public MethodInfo GetFunction(string name)
        {
            MethodInfo info;
            if (!functions.TryGetValue(name, out info)) return null;
            return info;
        }
    }
}
