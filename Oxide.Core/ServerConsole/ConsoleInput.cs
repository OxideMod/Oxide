using System;
using System.Linq;

namespace Oxide.Core.ServerConsole
{
    public class ConsoleInput
    {
        public string InputString = string.Empty;
        private float _nextUpdate;
        public event Action<string> OnInputText;
        public string[] StatusText = {string.Empty, string.Empty, string.Empty};

        public int LineWidth => Console.BufferWidth;

        public bool Valid => Console.BufferWidth > 0;

        public Func<string, string[]> Completion;

        public void ClearLine(int numLines)
        {
            Console.CursorLeft = 0;
            Console.Write(new string(' ', LineWidth*numLines));
            Console.CursorTop = Console.CursorTop - numLines;
            Console.CursorLeft = 0;
        }

        public void RedrawInputLine()
        {
            if (_nextUpdate - 0.45f > Interface.Oxide.Now) return;
            Console.ForegroundColor = ConsoleColor.White;
            Console.CursorTop = Console.CursorTop + 1;
            for (var i = 0; i < StatusText.Length; i++)
            {
                Console.CursorLeft = 0;
                Console.Write(StatusText[i].PadRight(LineWidth));
            }
            Console.CursorTop = Console.CursorTop - (StatusText.Length + 1);
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
            switch (consoleKeyInfo.Key)
            {
                case ConsoleKey.Enter:
                    ClearLine(StatusText.Length);
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine(string.Concat("> ", InputString));
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
                case ConsoleKey.Tab:
                    var results = Completion?.Invoke(InputString);
                    if (results == null || results.Length == 0) return;
                    if (results.Length > 1)
                    {
                        ClearLine(StatusText.Length + 1);
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