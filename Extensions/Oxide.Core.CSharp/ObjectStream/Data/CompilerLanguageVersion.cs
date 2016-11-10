using System;

namespace ObjectStream.Data
{
    [Serializable]
    internal enum CompilerLanguageVersion
    {
        ISO_1 = 1,
        ISO_2 = 2,
        V_3 = 3,
        V_4 = 4,
        V_5 = 5,
        V_6 = 6,
        Experimental = 100,

        Default = V_6
    }
}
