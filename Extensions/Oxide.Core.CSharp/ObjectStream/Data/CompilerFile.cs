using System;
using System.IO;

namespace ObjectStream.Data
{
    [Serializable]
    internal class CompilerFile
    {
        public string Name { get; set; }
        public byte[] Data { get; set; }

        internal CompilerFile(string name, byte[] data)
        {
            Name = name;
            Data = data;
        }

        internal CompilerFile(string directory, string name)
        {
            Name = name;
            Data = File.ReadAllBytes(Path.Combine(directory, Name));
        }

        internal CompilerFile(string path)
        {
            Name = Path.GetFileName(path);
            Data = File.ReadAllBytes(path);
        }
    }
}
