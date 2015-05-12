using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Oxide.Core.ServerConsole
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

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        public static bool Check()
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    return GetConsoleWindow() == IntPtr.Zero;
            }
            return false;
        }

        public void SetTitle(string title)
        {
            if (title != null) SetConsoleTitleA(title);
        }

        public void Initialize()
        {
            if (!Check()) return;
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) AllocConsole();
            _oldOutput = Console.Out;
            try
            {
                var stdHandle = GetStdHandle(STD_OUTPUT_HANDLE);
                var fileStream = new FileStream(new Microsoft.Win32.SafeHandles.SafeFileHandle(stdHandle, true), FileAccess.Write);
                var streamWriter = new StreamWriter(fileStream, Encoding.ASCII) { AutoFlush = true };
                Console.SetOut(streamWriter);
            }
            catch (Exception exception)
            {
                Interface.Oxide.LogException("Couldn't redirect output: ", exception);
            }
        }

        public void Shutdown()
        {
            Console.SetOut(_oldOutput);
            FreeConsole();
        }
    }
}