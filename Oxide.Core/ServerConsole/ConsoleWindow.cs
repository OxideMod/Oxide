using Microsoft.Win32.SafeHandles;
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
        private TextWriter oldOutput;
        private Encoding oldEncoding;

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool AttachConsole(uint dwProcessId);

        [DllImport("kernel32.dll", CharSet = CharSet.None, ExactSpelling = false, SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleOutputCP(uint wCodePageId);

        [DllImport("kernel32.dll")]
        private static extern bool SetConsoleTitle(string lpConsoleTitle);

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
            if (title != null) SetConsoleTitle(title);
        }

        public bool Initialize()
        {
            if (!AttachConsole(ATTACH_PARENT_PROCESS)) AllocConsole();
            if (GetConsoleWindow() == IntPtr.Zero)
            {
                FreeConsole();
                return false;
            }
            oldOutput = Console.Out;
            oldEncoding = Console.OutputEncoding;
            var encoding = new UTF8Encoding(false);
            SetConsoleOutputCP((uint)encoding.CodePage);
            Console.OutputEncoding = encoding;
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
            Console.SetOut(new StreamWriter(outStream, encoding) { AutoFlush = true });
            return true;
        }

        public void Shutdown()
        {
            if (oldOutput != null) Console.SetOut(oldOutput);
            if (oldEncoding != null)
            {
                SetConsoleOutputCP((uint)oldEncoding.CodePage);
                Console.OutputEncoding = oldEncoding;
            }
            FreeConsole();
        }
    }
}
