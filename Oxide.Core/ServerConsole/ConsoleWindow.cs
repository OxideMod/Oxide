using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using Microsoft.Win32.SafeHandles;

namespace Oxide.Core.ServerConsole
{
    public class ConsoleWindow
    {
        private const uint ATTACH_PARENT_PROCESS = 0xFFFFFFFF;
        private const int STD_OUTPUT_HANDLE = -11;
        private TextWriter _oldOutput;
        private Encoding _oldEncoding;

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleOutputCP(uint wCodePageID);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        public static bool Check(bool force = false)
        {
            switch (Environment.OSVersion.Platform)
            {
                case PlatformID.Win32NT:
                case PlatformID.Win32S:
                case PlatformID.Win32Windows:
                    var pDll = GetModuleHandle("ntdll.dll");
                    if (pDll == IntPtr.Zero) return false;
                    return GetProcAddress(pDll, "wine_get_version") == IntPtr.Zero && (force || GetConsoleWindow() == IntPtr.Zero);
            }
            return false;
        }

        public void SetTitle(string title)
        {
            if (title != null) Console.Title = title;
        }

        public void Initialize()
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) AllocConsole();
            _oldOutput = Console.Out;
            _oldEncoding = Console.OutputEncoding;
            SetConsoleOutputCP((uint)Encoding.UTF8.CodePage);
            Console.OutputEncoding = Encoding.UTF8;
            Stream outStream;
            try
            {
                var safeFileHandle = new SafeFileHandle(GetStdHandle(STD_OUTPUT_HANDLE), true);
                outStream = new FileStream(safeFileHandle, FileAccess.Write);
            }
            catch (Exception)
            {
                outStream = Console.OpenStandardOutput();
            }
            Console.SetOut(new StreamWriter(outStream, Encoding.UTF8) { AutoFlush = true });
        }

        public void Shutdown()
        {
            Console.SetOut(_oldOutput);
            SetConsoleOutputCP((uint)_oldEncoding.CodePage);
            Console.OutputEncoding = _oldEncoding;
            FreeConsole();
        }
    }
}