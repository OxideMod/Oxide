using System;

namespace ObjectStream.Data
{
    [Serializable]
    internal enum CompilerMessageType
    {
        Assembly,
        Compile,
        Error,
        Exit,
        Ready
    }
}
