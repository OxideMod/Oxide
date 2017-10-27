extern alias Oxide;

using Oxide::ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;

namespace Oxide.Core
{
    public class ProtoStorage
    {
        public static IEnumerable<string> GetFiles(string subDirectory)
        {
            var directory = GetFileDataPath(subDirectory.Replace("..", ""));
            if (!Directory.Exists(directory)) yield break;
            foreach (var file in Directory.GetFiles(directory, "*.data")) yield return Utility.GetFileNameWithoutExtension(file);
        }

        public static T Load<T>(params string[] subPaths)
        {
            var name = GetFileName(subPaths);
            var path = GetFileDataPath(name);
            try
            {
                if (File.Exists(path))
                {
                    T data;
                    using (var file = File.OpenRead(path)) data = Serializer.Deserialize<T>(file);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to load protobuf data from {name}", ex);
            }
            return default(T);
        }

        public static void Save<T>(T data, params string[] subPaths)
        {
            var name = GetFileName(subPaths);
            var path = GetFileDataPath(name);
            var directory = Path.GetDirectoryName(path);
            try
            {
                if (directory != null && !Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using (var file = File.Open(path, FileMode.Create)) Serializer.Serialize(file, data);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException($"Failed to save protobuf data to {name}", ex);
            }
        }

        public static bool Exists(params string[] subPaths) => File.Exists(GetFileDataPath(GetFileName(subPaths)));

        public static string GetFileName(params string[] subPaths)
        {
            return string.Join(Path.DirectorySeparatorChar.ToString(), subPaths).Replace("..", "") + ".data";
        }

        public static string GetFileDataPath(string name) => Path.Combine(Interface.Oxide.DataDirectory, name);
    }
}
