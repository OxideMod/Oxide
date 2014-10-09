using System;
using System.Collections.Generic;
using System.Linq;

using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Rust.Libraries
{
    /// <summary>
    /// A library containing utility shortcut functions for rust
    /// </summary>
    public class Rust : Library
    {
        /// <summary>
        /// Returns if this library should be loaded into the global namespace
        /// </summary>
        public override bool IsGlobal { get { return false; } }


    }
}
