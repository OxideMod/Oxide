using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Core.ServerConsole
{
    public class ConsoleInput
    {
        private string inputString = string.Empty;
        private readonly List<string> inputHistory = new List<string>();
        private int inputHistoryIndex;
        private float nextUpdate;

        internal event Action<string> OnInputText;

        internal readonly string[] StatusTextLeft = { string.Empty, string.Empty, string.Empty, string.Empty };
        internal readonly string[] StatusTextRight = { string.Empty, string.Empty, string.Empty, string.Empty };
        internal readonly ConsoleColor[] StatusTextLeftColor = { ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
        internal readonly ConsoleColor[] StatusTextRightColor = { ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };

        public int LineWidth => Console.BufferWidth;

        public bool Valid => Console.BufferWidth > 0;

        public Func<string, string[]> Completion;

        public void ClearLine(int numLines)
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', LineWidth * numLines));
            Console.CursorTop = Console.CursorTop - numLines;
            Console.CursorLeft = 0;
        }

        public void RedrawInputLine()
        {
            if (nextUpdate - 0.45f > Interface.Oxide.Now || LineWidth <= 0) return;
            try
            {
                Console.CursorTop = Console.CursorTop + 1;
                for (var i = 0; i < StatusTextLeft.Length; i++)
                {
                    if (!Interface.Oxide.Config.Console.ShowStatusBar) break;
                    Console.CursorLeft = 0;
                    Console.ForegroundColor = StatusTextLeftColor[i];
                    Console.Write(StatusTextLeft[i].Substring(0, Math.Min(StatusTextLeft[i].Length, LineWidth - 1)));
                    Console.ForegroundColor = StatusTextRightColor[i];
                    Console.Write(StatusTextRight[i].PadRight(LineWidth));
                }
                Console.CursorTop = Console.CursorTop - (Interface.Oxide.Config.Console.ShowStatusBar ? StatusTextLeft.Length + 1 : 1);
                Console.CursorLeft = 0;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.ForegroundColor = ConsoleColor.Green;
                ClearLine(1);
                if (inputString.Length == 0)
                {
                    Console.ForegroundColor = ConsoleColor.Gray;
                    return;
                }
                Console.Write(inputString.Length >= LineWidth - 2 ? inputString.Substring(inputString.Length - (LineWidth - 2)) : inputString);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception e)
            {
                Interface.Oxide.LogException("RedrawInputLine: ", e);
            }
        }

        public void Update()
        {
            if (!Valid) return;
            if (nextUpdate < Interface.Oxide.Now)
            {
                RedrawInputLine();
                nextUpdate = Interface.Oxide.Now + 0.5f;
            }
            try
            {
                if (!Console.KeyAvailable) return;
            }
            catch (Exception)
            {
                return;
            }
            var consoleKeyInfo = Console.ReadKey();
            if (consoleKeyInfo.Key != ConsoleKey.DownArrow && consoleKeyInfo.Key != ConsoleKey.UpArrow) inputHistoryIndex = 0;
            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Enter:
                    ClearLine(Interface.Oxide.Config.Console.ShowStatusBar ? StatusTextLeft.Length : 1);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Concat("> ", inputString));
                    inputHistory.Insert(0, inputString);
                    if (inputHistory.Count > 50) inputHistory.RemoveRange(50, inputHistory.Count - 50);
                    var str = inputString;
                    inputString = string.Empty;
                    OnInputText?.Invoke(str);
                    RedrawInputLine();
                    return;

                case ConsoleKey.Backspace:
                    if (inputString.Length < 1) return;
                    inputString = inputString.Substring(0, inputString.Length - 1);
                    RedrawInputLine();
                    return;

                case ConsoleKey.Escape:
                    inputString = string.Empty;
                    RedrawInputLine();
                    return;

                case ConsoleKey.UpArrow:
                    if (inputHistory.Count == 0) return;
                    if (inputHistoryIndex < 0) inputHistoryIndex = 0;
                    if (inputHistoryIndex >= inputHistory.Count - 1)
                    {
                        inputHistoryIndex = inputHistory.Count - 1;
                        inputString = inputHistory[inputHistoryIndex];
                        RedrawInputLine();
                        return;
                    }
                    inputString = inputHistory[inputHistoryIndex++];
                    RedrawInputLine();
                    return;

                case ConsoleKey.DownArrow:
                    if (inputHistory.Count == 0) return;
                    if (inputHistoryIndex >= inputHistory.Count - 1) inputHistoryIndex = inputHistory.Count - 2;
                    inputString = inputHistoryIndex < 0 ? string.Empty : inputHistory[inputHistoryIndex--];
                    RedrawInputLine();
                    return;

                case ConsoleKey.Tab:
                    var results = Completion?.Invoke(inputString);
                    if (results == null || results.Length == 0) return;
                    if (results.Length > 1)
                    {
                        ClearLine(Interface.Oxide.Config.Console.ShowStatusBar ? StatusTextLeft.Length + 1 : 1);
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        var lowestDiff = results.Max(r => r.Length);
                        for (var index = 0; index < results.Length; index++)
                        {
                            var result = results[index];
                            if (index > 0)
                            {
                                var diff = GetFirstDiffIndex(results[0], result);
                                if (diff > 0 && diff < lowestDiff)
                                    lowestDiff = diff;
                            }
                            Console.WriteLine(result);
                        }
                        if (lowestDiff > 0)
                            inputString = results[0].Substring(0, lowestDiff);
                        RedrawInputLine();
                        return;
                    }
                    inputString = results[0];
                    RedrawInputLine();
                    return;
            }
            if (consoleKeyInfo.KeyChar == 0) return;
            inputString = string.Concat(inputString, consoleKeyInfo.KeyChar);
            RedrawInputLine();
        }

        private static int GetFirstDiffIndex(string str1, string str2)
        {
            if (str1 == null || str2 == null) return -1;
            var length = Math.Min(str1.Length, str2.Length);
            for (var index = 0; index < length; index++) if (str1[index] != str2[index]) return index;
            return length;
        }
    }
}
