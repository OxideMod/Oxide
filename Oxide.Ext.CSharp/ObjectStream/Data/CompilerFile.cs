using System;

namespace ObjectStream.Data
{
    [Serializable]
    internal class CompilerFile
    {
        public string Name { get; set; }
        public byte[] Data { get; set; }
    }
}