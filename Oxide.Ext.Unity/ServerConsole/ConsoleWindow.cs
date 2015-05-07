using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

using UnityEngine;

namespace Oxide.Ext.Unity.ServerConsole
{
    public class ConsoleWindow
    {
        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        private const int STD_OUTPUT_HANDLE = -11;
        private TextWriter _oldOutput;

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, ExactSpelling = false, SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false)]
        private static extern bool SetConsoleTitleA(string lpConsoleTitle);

        public void Initialize()
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) AllocConsole();
            _oldOutput = Console.Out;
            try
            {
                var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                var fileStream = new FileStream(new SafeFileHandle(stdHandle, true), FileAccess.Write);
                var streamWriter = new StreamWriter(fileStream, Encoding.ASCII) { AutoFlush = true };
                Console.SetOut(streamWriter);
            }
            catch (Exception exception)
            {
                Debug.Log(string.Concat("Couldn't redirect output: ", exception.Message));
            }
        }

        public void Shutdown()
        {
            Console.SetOut(_oldOutput);
            FreeConsole();
        }
    }
}