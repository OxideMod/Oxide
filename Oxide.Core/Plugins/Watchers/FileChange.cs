using System.IO;

namespace Oxide.Core.Plugins.Watchers
{
    /// <summary>
    /// Represents a file change
    /// </summary>
    public sealed class FileChange
    {
        /// <summary>
        /// Gets the Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the ChangeType
        /// </summary>
        public WatcherChangeTypes ChangeType { get; private set; }

        /// <summary>
        /// Initializes a new instance of the FileChange class
        /// </summary>
        /// <param name="name"></param>
        /// <param name="changeType"></param>
        public FileChange(string name, WatcherChangeTypes changeType)
        {
            // Initialize
            Name = name;
            ChangeType = changeType;
        }
    }
}
