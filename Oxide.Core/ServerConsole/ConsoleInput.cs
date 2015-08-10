using System;
using System.Collections.Generic;
using System.Linq;

namespace Oxide.Core.ServerConsole
{
    public class ConsoleInput
    {
        public string InputString = string.Empty;
        private readonly List<string> _inputHistory = new List<string>();
        private int _inputHistoryIndex;
        private float _nextUpdate;
        internal event Action<string> OnInputText;
        internal string[] StatusTextLeft = { string.Empty, string.Empty, string.Empty, string.Empty };
        internal string[] StatusTextRight = { string.Empty, string.Empty, string.Empty, string.Empty };
        internal ConsoleColor[] StatusTextLeftColor = { ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };
        internal ConsoleColor[] StatusTextRightColor = { ConsoleColor.White, ConsoleColor.White, ConsoleColor.White, ConsoleColor.White };

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
            if (_nextUpdate - 0.45f > Interface.Oxide.Now) return;
            Console.CursorTop = Console.CursorTop + 1;
            for (var i = 0; i < StatusTextLeft.Length; i++)
            {
                Console.CursorLeft = 0;
                Console.ForegroundColor = StatusTextLeftColor[i];
                Console.Write(StatusTextLeft[i]);
                Console.ForegroundColor = StatusTextRightColor[i];
                Console.Write(StatusTextRight[i].PadRight(LineWidth));
            }
            Console.CursorTop = Console.CursorTop - (StatusTextLeft.Length + 1);
            Console.CursorLeft = 0;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.ForegroundColor = ConsoleColor.Green;
            ClearLine(1);
            if (InputString.Length == 0)
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                return;
            }
            Console.Write(InputString.Length >= LineWidth - 2 ? InputString.Substring(InputString.Length - (LineWidth - 2)) : InputString);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void Update()
        {
            if (!Valid) return;
            if (_nextUpdate < Interface.Oxide.Now)
            {
                RedrawInputLine();
                _nextUpdate = Interface.Oxide.Now + 0.5f;
            }
            try
            {
                if (!Console.KeyAvailable)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            var consoleKeyInfo = Console.ReadKey();
            if (consoleKeyInfo.Key != ConsoleKey.DownArrow && consoleKeyInfo.Key != ConsoleKey.UpArrow) _inputHistoryIndex = 0;
            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Enter:
                    ClearLine(StatusTextLeft.Length);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Concat("> ", InputString));
                    _inputHistory.Insert(0, InputString);
                    if (_inputHistory.Count > 50) _inputHistory.RemoveRange(50, _inputHistory.Count - 50);
                    var str = InputString;
                    InputString = string.Empty;
                    if (OnInputText != null) OnInputText(str);
                    RedrawInputLine();
                    return;
                case ConsoleKey.Backspace:
                    if (InputString.Length < 1) return;
                    InputString = InputString.Substring(0, InputString.Length - 1);
                    RedrawInputLine();
                    return;
                case ConsoleKey.Escape:
                    InputString = string.Empty;
                    RedrawInputLine();
                    return;
                case ConsoleKey.UpArrow:
                    if (_inputHistory.Count == 0) return;
                    if (_inputHistoryIndex < 0) _inputHistoryIndex = 0;
                    if (_inputHistoryIndex >= _inputHistory.Count - 1)
                    {
                        _inputHistoryIndex = _inputHistory.Count - 1;
                        InputString = _inputHistory[_inputHistoryIndex];
                        RedrawInputLine();
                        return;
                    }
                    InputString = _inputHistory[_inputHistoryIndex++];
                    RedrawInputLine();
                    return;
                case ConsoleKey.DownArrow:
                    if (_inputHistory.Count == 0) return;
                    if (_inputHistoryIndex >= _inputHistory.Count - 1) _inputHistoryIndex = _inputHistory.Count - 2;
                    if (_inputHistoryIndex < 0)
                    {
                        InputString = string.Empty;
                        RedrawInputLine();
                        return;
                    }
                    InputString = _inputHistory[_inputHistoryIndex--];
                    RedrawInputLine();
                    return;
                case ConsoleKey.Tab:
                    var results = Completion?.Invoke(InputString);
                    if (results == null || results.Length == 0) return;
                    if (results.Length > 1)
                    {
                        ClearLine(StatusTextLeft.Length + 1);
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
                            InputString = results[0].Substring(0, lowestDiff);
                        RedrawInputLine();
                        return;
                    }
                    InputString = results[0];
                    RedrawInputLine();
                    return;
            }
            if (consoleKeyInfo.KeyChar == 0) return;
            InputString = string.Concat(InputString, consoleKeyInfo.KeyChar);
            RedrawInputLine();
        }

        private static int GetFirstDiffIndex(string str1, string str2)
        {
            if (str1 == null || str2 == null) return -1;
            var length = Math.Min(str1.Length, str2.Length);
            for (var index = 0; index < length; index++)
                if (str1[index] != str2[index]) return index;
            return length;
        }
    }
}
