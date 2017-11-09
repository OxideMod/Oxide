using System;
using System.IO;

namespace Oxide.Core.Libraries.Covalence
{
    public class SaveInfo
    {
        private readonly Time time = Interface.Oxide.GetLibrary<Time>();
        private readonly string FullPath;

        /// <summary>
        /// The name of the save file
        /// </summary>
        public string SaveName { get; private set; }

        /// <summary>
        /// Get the save creation time in server local time
        /// </summary>
        public DateTime CreationTime { get; private set; }

        /// <summary>
        /// Get the save creation time in unix format
        /// </summary>
        public uint CreationTimeUnix { get; private set; }

        public void Refresh()
        {
            if (!File.Exists(FullPath)) return;

            CreationTime = File.GetCreationTime(FullPath);
            CreationTimeUnix = time.GetUnixFromDateTime(CreationTime);
        }

        private SaveInfo(string filepath)
        {
            FullPath = filepath;
            SaveName = Utility.GetFileNameWithoutExtension(filepath);
            Refresh();
        }

        /// <summary>
        /// Creates a new SaveInfo for a specifed file
        /// </summary>
        /// <param name="filepath">FullPath to the save file</param>
        /// <returns></returns>
        public static SaveInfo Create(string filepath)
        {
            if (!File.Exists(filepath)) return null;

            return new SaveInfo(filepath);
        }
    }
}
