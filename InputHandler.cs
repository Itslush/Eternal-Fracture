using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static EternalFracture.NetworkManagementWMI;
using static EternalFracture.ConsoleLogging;

namespace EternalFracture
{
    class InputHandler
    {
        public static bool UseAsyncKeyState = true;
        private static DateTime _lastToggleTime = DateTime.MinValue;
        private static readonly TimeSpan ToggleCooldown = TimeSpan.FromSeconds(1);
        public static Dictionary<int, bool> KeyProcessed { get; } = new()
        {
            {0x43, false },{ 0x44, false }, { 0x45, false }, { 0x4C, false }, { 0x51, false }, { 0x54, false }, {0x53, false}, {0x52, false}
        };
        public static Dictionary<ConsoleKey, Action> KeyActions { get; } = new()
        {
            { ConsoleKey.D, NetworkManagementWMI.DisableAllNetworkInterfaces},
            { ConsoleKey.E, NetworkManagementWMI.EnableAllNetworkInterfaces },
            { ConsoleKey.L, ToggleLoggingLevel },
            { ConsoleKey.Q, () => { Log("Exiting...", ErrorColor); Environment.Exit(0); } },
            { ConsoleKey.T, InputHandler.ToggleInputMode },
            { ConsoleKey.S, NetworkManagementWMI.SetDisableDuration },
            { ConsoleKey.R, () => NetworkManagementWMI.TempDisableNetInterfaces(NetworkManagementWMI.disableDurationSeconds) },
            { ConsoleKey.C, ConsoleLogging.ClearLogs},
        };
        public static void CheckAsyncKeyState()
        {
            CheckKeyState(0x43, ConsoleLogging.ClearLogs);
            CheckKeyState(0x44, NetworkManagementWMI.DisableAllNetworkInterfaces);
            CheckKeyState(0x45, NetworkManagementWMI.EnableAllNetworkInterfaces);
            CheckKeyState(0x4C, ConsoleLogging.ToggleLoggingLevel);
            CheckKeyState(0x51, () => { ConsoleLogging.Log("Exiting...", ConsoleLogging.ErrorColor); Environment.Exit(0); });
            CheckKeyState(0x54, ToggleInputMode);
            CheckKeyState(0x53, SetDisableDuration);
            CheckKeyState(0x52, () => TempDisableNetInterfaces(NetworkManagementWMI.disableDurationSeconds));
        }
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);
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
        public static void CheckConsoleReadKey()
        {
            var keyInfo = Console.ReadKey(intercept: true).Key;

            if (KeyActions.TryGetValue(keyInfo, out var action))
            {
                action();
            }
        }
        private static void ToggleInputMode()
        {
            if (DateTime.Now - _lastToggleTime < ToggleCooldown) return;

            _lastToggleTime = DateTime.Now;
            UseAsyncKeyState = !UseAsyncKeyState;
            Log($"Input mode toggled. Using {(UseAsyncKeyState ? "GetAsyncKeyState" : "Console.ReadKey")} for input.", InfoColor);
        }
    }
}
