using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Oxide.Core
{
    /// <summary>
    /// Represents a command line string in managed form
    /// </summary>
    public sealed class CommandLine
    {
        // The flags and variables of this command line
        private string[] flags;
        private Variable[] variables;

        /// <summary>
        /// Represents a variable
        /// </summary>
        private struct Variable
        {
            public string Name;
            public string Value;
        }

        /// <summary>
        /// Initializes a new instance of the CommandLine class
        /// </summary>
        /// <param name="cmdline"></param>
        public CommandLine(string cmdline)
        {
            // Split into args
            List<string> arglist = new List<string>();
            StringBuilder curarg = new StringBuilder();
            bool insidelongarg = false;
            for (int i = 0; i < cmdline.Length; i++)
            {
                char c = cmdline[i];
                if (char.IsWhiteSpace(c) && !insidelongarg)
                {
                    if (curarg.Length > 0)
                    {
                        arglist.Add(curarg.ToString());
                        curarg = new StringBuilder();
                    }
                }
                else if (c == '"')
                {
                    if (insidelongarg)
                    {
                        insidelongarg = false;
                        arglist.Add(curarg.ToString());
                        curarg = new StringBuilder();
                    }
                    else if (curarg.Length == 0)
                        insidelongarg = true;
                }
                else
                    curarg.Append(c);
            }
            if (curarg.Length > 0) arglist.Add(curarg.ToString());
            
            // Build flags and variables arrays
            List<string> flaglist = new List<string>();
            List<Variable> varlist = new List<Variable>();
            for (int i = 0; i < arglist.Count; i++)
            {
                string arg = arglist[i];
                if (arg.Length > 0)
                {
                    char prefix = arg[0];
                    switch (prefix)
                    {
                        case '-':
                            flaglist.Add(arg.Substring(1));
                            break;
                        case '+':
                            if (i < arglist.Count - 1)
                                varlist.Add(new Variable() { Name = arg.Substring(1), Value = arglist[++i] });
                            break;
                    }
                }
            }
            flags = flaglist.ToArray();
            variables = varlist.ToArray();
        }

        /// <summary>
        /// Returns if this command line has the specified flag (prefixed with -)
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public bool HasFlag(string flag)
        {
            // Search
            return flags.Contains(flag);
        }

        /// <summary>
        /// Returns if this command line has the specified variable (prefixed with +)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasVariable(string name)
        {
            // Search
            return variables.Any((v) => v.Name == name);
        }

        /// <summary>
        /// Gets the value for the specified variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetVariable(string name)
        {
            try
            {
                return variables.Single((v) => v.Name == name).Value;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
