using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static EternalFracture.NetworkManagement;
using static EternalFracture.ConsoleLogging;

namespace EternalFracture
{
    internal class InputHandler
    {
        public static bool UseAsyncKeyState = true;
        public static Dictionary<int, bool> KeyProcessed { get; } = new()
        {
            { 0x44, false }, { 0x45, false }, { 0x4C, false }, { 0x51, false }, { 0x54, false }
        };

        public static Dictionary<ConsoleKey, Action> KeyActions { get; } = new()
        {
            { ConsoleKey.D, () => NetworkManagement.DisableAllNetworkInterfaces() },
            { ConsoleKey.E, NetworkManagement.EnableAllNetworkInterfaces },
            { ConsoleKey.L, ToggleLoggingLevel },
            { ConsoleKey.Q, () => { Log("Exiting...", ErrorColor); Environment.Exit(0); } },
            { ConsoleKey.T, InputHandler.ToggleInputMode },
            { ConsoleKey.S, NetworkManagement.SetDisableDuration },
            { ConsoleKey.R, () => NetworkManagement.TempDisableNetInterfaces(NetworkManagement.disableDurationSeconds) }
        };

        [DllImport("User32.dll", EntryPoint = "GetAsyncKeyState")]
        private static extern short GetAsyncKeyState(int vKey);

        public static void CheckAsyncKeyState()
        {
            CheckKeyState(0x44, () => NetworkManagement.DisableAllNetworkInterfaces());
            CheckKeyState(0x45, NetworkManagement.EnableAllNetworkInterfaces);
            CheckKeyState(0x4C, ConsoleLogging.ToggleLoggingLevel);
            CheckKeyState(0x51, () =>
            {
                ConsoleLogging.Log("Exiting...", ConsoleLogging.ErrorColor);
                Environment.Exit(0);
            });
            CheckKeyState(0x54, ToggleInputMode);
        }

        public static void CheckConsoleReadKey()
        {
            var key = Console.ReadKey(true).Key;

            if (KeyActions.TryGetValue(key, out var action))
            {
                action();
            }
        }

        private static void CheckKeyState(int vKey, Action action)
        {
            if ((GetAsyncKeyState(vKey) & 0x8000) > 0)
            {
                if (!KeyProcessed[vKey])
                {
                    action();
                    KeyProcessed[vKey] = true;
                }
            }
            else
            {
                KeyProcessed[vKey] = false;
            }
        }

        private static void ToggleInputMode()
        {
            ConsoleLogging.Log("Toggling Input Mode...", ConsoleLogging.WarningColor);
            UseAsyncKeyState = !UseAsyncKeyState;
        }

        /*private static void LogWithGradient(string message)
        {
            var gradientColors = new[] { ConsoleColor.Red, ConsoleColor.Yellow, ConsoleColor.Green };
            var messageLength = message.Length;
            var colorStep = (double)messageLength / gradientColors.Length;

            for (int i = 0; i < messageLength; i++)
            {
                var colorIndex = (int)(i / colorStep);
                Console.ForegroundColor = gradientColors[colorIndex];
                Console.Write(message[i]);
            }

            Console.WriteLine();
            Console.ResetColor();
        }*/
    }
}