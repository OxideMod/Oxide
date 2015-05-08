using System;

using Oxide.Unity;

using UnityEngine;

namespace Oxide.Ext.Unity.ServerConsole
{
    public class ServerConsole : MonoBehaviour
    {
        private readonly ConsoleWindow _console = new ConsoleWindow();
        private readonly ConsoleInput _input = new ConsoleInput();
        private float _nextUpdate;

        public Func<string> Status1Left;
        public Func<string> Status1Right;
        public Func<string> Status2Left;
        public Func<string> Status2Right;

        private string status1Left
        {
            get
            {
                return Status1Left != null ? Status1Left() : "status1left";
            }
        }

        private string status1Right
        {
            get
            {
                return (Status1Right != null ? Status1Right() : "status1right").PadLeft(_input.LineWidth - 1);
            }
        }

        private string status2Left
        {
            get
            {
                return Status2Left != null ? Status2Left() : "status2left";
            }
        }

        private string status2Right
        {
            get
            {
                return (Status2Right != null ? Status2Right() : "status2right").PadLeft(_input.LineWidth - 1);
            }
        }

        private static string GetStatus(string left, string right)
        {
            return string.Concat(left, (left.Length >= right.Length ? string.Empty : right.Substring(left.Length)));
        }

        public void HandleLog(string message, string stackTrace, LogType type)
        {
            if (type == LogType.Warning)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else if (type != LogType.Error)
                Console.ForegroundColor = ConsoleColor.Gray;
            else
                Console.ForegroundColor = ConsoleColor.Red;
            _input.ClearLine(_input.StatusText.Length);
            Console.WriteLine(message);
            _input.RedrawInputLine();
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        private void OnDisable()
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
            Console.WriteLine("Command: " + obj);
        }

        public static void PrintColoured(params object[] objects)
        {
            if (UnityScript.ServerConsole == null) return;
            UnityScript.ServerConsole._input.ClearLine(UnityScript.ServerConsole._input.StatusText.Length);
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
            UnityScript.ServerConsole._input.RedrawInputLine();
        }

        private void Update()
        {
            UpdateStatus();
            _input.Update();
        }

        private void UpdateStatus()
        {
            if (_nextUpdate > Time.realtimeSinceStartup) return;
            _nextUpdate = Time.realtimeSinceStartup + 0.33f;
            if (!_input.Valid) return;
            _input.StatusText[0] = string.Empty;
            _input.StatusText[1] = GetStatus(status1Left, status1Right);
            _input.StatusText[2] = GetStatus(status2Left, status2Right);
        }
    }
}