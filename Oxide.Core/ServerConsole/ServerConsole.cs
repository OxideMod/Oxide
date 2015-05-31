using System;

namespace Oxide.Core.ServerConsole
{
    public class ServerConsole
    {
        private readonly ConsoleWindow _console = new ConsoleWindow();
        private readonly ConsoleInput _input = new ConsoleInput();
        private float _nextUpdate;
        private float _nextTitleUpdate;

        public event Action<string> Input;

        public Func<string> Status1Left;
        public Func<string> Status1Right;
        public Func<string> Status2Left;
        public Func<string> Status2Right;

        public Func<string> Title;

        public Func<string, string[]> Completion
        {
            get { return _input.Completion; }
            set { _input.Completion = value; }
        }

        private string status1Left => Status1Left != null ? Status1Left() : "status1left";
        private string status1Right => (Status1Right != null ? Status1Right() : "status1right").PadLeft(_input.LineWidth - 1);
        private string status2Left => Status2Left != null ? Status2Left() : "status2left";
        private string status2Right => (Status2Right != null ? Status2Right() : "status2right").PadLeft(_input.LineWidth - 1);

        private string title => Title != null ? Title() : null;

        private static string GetStatus(string left, string right)
        {
            return string.Concat(left, (left.Length >= right.Length ? string.Empty : right.Substring(left.Length)));
        }

        public void AddMessage(string message, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            _input.ClearLine(_input.StatusText.Length);
            Console.WriteLine(message);
            _input.RedrawInputLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public void OnDisable()
        {
            _input.OnInputText -= OnInputText;
            _console.Shutdown();
        }

        public void OnEnable()
        {
            _console.Initialize();
            _input.OnInputText += OnInputText;
            _input.ClearLine(Console.WindowHeight);
            for (var i = 0; i < Console.WindowHeight; i++)
            {
                Console.WriteLine(string.Empty);
            }
        }

        private void OnInputText(string obj)
        {
            if (Input != null) Input(obj);
        }

        public static void PrintColoured(params object[] objects)
        {
            if (Interface.Oxide.ServerConsole == null) return;
            Interface.Oxide.ServerConsole._input.ClearLine(Interface.Oxide.ServerConsole._input.StatusText.Length);
            for (var i = 0; i < objects.Length; i++)
            {
                if (i%2 != 0)
                {
                    Console.Write((string) objects[i]);
                }
                else
                {
                    Console.ForegroundColor = (ConsoleColor) ((int) objects[i]);
                }
            }
            if (Console.CursorLeft != 0)
            {
                Console.CursorTop = Console.CursorTop + 1;
            }
            Interface.Oxide.ServerConsole._input.RedrawInputLine();
        }

        public void Update()
        {
            UpdateStatus();
            _input.Update();
            if (_nextTitleUpdate > Interface.Oxide.Now) return;
            _nextTitleUpdate = Interface.Oxide.Now + 1f;
            _console.SetTitle(title);
        }

        private void UpdateStatus()
        {
            if (_nextUpdate > Interface.Oxide.Now) return;
            _nextUpdate = Interface.Oxide.Now + 0.66f;
            if (!_input.Valid) return;
            _input.StatusText[0] = string.Empty;
            _input.StatusText[1] = GetStatus(status1Left, status1Right);
            _input.StatusText[2] = GetStatus(status2Left, status2Right);
        }
    }
}