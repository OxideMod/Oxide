using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Oxide.Core
{
    /// <summary>
    /// Represents a command line string in managed form
    /// </summary>
    public sealed class CommandLine
    {
        // The flags and variables of this command line
        private readonly Dictionary<string, string> variables = new Dictionary<string, string>();

        /// <summary>
        /// Initializes a new instance of the CommandLine class
        /// </summary>
        /// <param name="commandline"></param>
        public CommandLine(string[] commandline)
        {
            var cmdline = string.Empty;
            var key = string.Empty;

            foreach (var str in commandline) cmdline += "\"" + str.Trim('/', '\\') + "\"";

            foreach (var str in Split(cmdline))
            {
                if (str.Length <= 0) continue;

                var val = str;
                if (str[0] == '-' || str[0] == '+')
                {
                    if (key != string.Empty && !variables.ContainsKey(key)) variables.Add(key, string.Empty);
                    key = val.Substring(1);
                }
                else if (key != string.Empty)
                {
                    if (!variables.ContainsKey(key))
                    {
                        if (key.Contains("dir")) val = val.Replace('/', '\\');
                        variables.Add(key, val);
                    }
                    key = string.Empty;
                }
            }

            if (key != string.Empty && !variables.ContainsKey(key)) variables.Add(key, string.Empty);
        }

        /// <summary>
        /// Split the commandline arguments
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public string[] Split(string input)
        {
            input = input.Replace("\\\"", "&qute;");
            var matchs = new Regex("\"([^\"]+)\"|'([^']+)'|\\S+").Matches(input);
            var strArray = new string[matchs.Count];
            for (var i = 0; i < matchs.Count; i++)
            {
                char[] trimChars = { ' ', '"' };
                strArray[i] = matchs[i].Groups[0].Value.Trim(trimChars);
                strArray[i] = strArray[i].Replace("&qute;", "\"");
            }

            return strArray;
        }

        /// <summary>
        /// Returns if this command line has the specified variable (prefixed with +)
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool HasVariable(string name) => variables.Any(v => v.Key == name);

        /// <summary>
        /// Gets the value for the specified variable
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public string GetVariable(string name)
        {
            try
            {
                return variables.Single(v => v.Key == name).Value;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets variable data for the specified argument
        /// </summary>
        /// <param name="var"></param>
        /// <param name="varname"></param>
        /// <param name="format"></param>
        public void GetArgument(string var, out string varname, out string format)
        {
            // Format is "folder/{variable}/otherfolder"
            var cmd = GetVariable(var);
            StringBuilder varnamesb = new StringBuilder(), formatsb = new StringBuilder();
            var invar = 0;
            foreach (var c in cmd)
            {
                switch (c)
                {
                    case '{':
                        invar++;
                        break;

                    case '}':
                        invar--;
                        if (invar == 0) formatsb.Append("{0}");
                        break;

                    default:
                        if (invar == 0)
                            formatsb.Append(c);
                        else
                            varnamesb.Append(c);
                        break;
                }
            }
            varname = varnamesb.ToString();
            format = formatsb.ToString();
        }
    }
}
