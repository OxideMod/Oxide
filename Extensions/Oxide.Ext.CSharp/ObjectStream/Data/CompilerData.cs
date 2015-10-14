using System;

namespace ObjectStream.Data
{
    [Serializable]
    internal class CompilerData
    {
        public CompilerData()
        {
            StdLib = false;
            Target = CompilerTarget.Library;
            Platform = CompilerPlatform.AnyCPU;
            Version = CompilerLanguageVersion.Default;
            LoadDefaultReferences = false;
            SdkVersion = "2";
        }
        public bool LoadDefaultReferences { get; set; }
        public string OutputFile { get; set; }
        public CompilerPlatform Platform { get; set; }
        public CompilerFile[] ReferenceFiles { get; set; }
        public string SdkVersion { get; set; }
        public CompilerFile[] SourceFiles { get; set; }
        public bool StdLib { get; set; }
        public CompilerTarget Target { get; set; }
        public CompilerLanguageVersion Version { get; set; }
    }
}
