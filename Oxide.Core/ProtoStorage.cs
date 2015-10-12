using System;
using System.Collections.Generic;
using System.IO;

using ProtoBuf;

namespace Oxide.Core
{
    public class ProtoStorage
    {
        public static IEnumerable<string> GetFiles(string sub_directory)
        {
            var directory = GetFileDataPath(sub_directory.Replace("..", ""));
            if (!Directory.Exists(directory)) yield break;
            foreach (var file in Directory.GetFiles(directory, "*.data"))
                yield return Utility.GetFileNameWithoutExtension(file);
        }

        public static T Load<T>(params string[] sub_paths)
        {
            var name = GetFileName(sub_paths);
            var path = GetFileDataPath(name);
            try
            {
                if (File.Exists(path))
                {
                    T data;
                    using (var file = File.OpenRead(path))
                        data = Serializer.Deserialize<T>(file);
                    return data;
                }
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Failed to load protobuf data from " + name, ex);
            }
            return default(T);
        }

        public static void Save<T>(T data, params string[] sub_paths)
        {
            var name = GetFileName(sub_paths);
            var path = GetFileDataPath(name);
            var directory = Path.GetDirectoryName(path);
            try
            {
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                using (var file = File.Open(path, FileMode.Create))
                    Serializer.Serialize(file, data);
            }
            catch (Exception ex)
            {
                Interface.Oxide.LogException("Failed to save protobuf data to " + name, ex);
            }
        }

        public static bool Exists(params string[] sub_paths)
        {
            return File.Exists(GetFileDataPath(GetFileName(sub_paths)));
        }

        public static string GetFileName(params string[] sub_paths)
        {
            return string.Join(Path.PathSeparator.ToString(), sub_paths).Replace("..", "") + ".data";
        }

        public static string GetFileDataPath(string name)
        {
            return Path.Combine(Interface.Oxide.DataDirectory, name);
        }
    }
}
