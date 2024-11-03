using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EternalFracture
{
    class ConsoleLogging
    {
        public static LogLevel _CurrentLogLevel = LogLevel.Detailed;
        public static LogLevel ReturnLogLevel { get { return _CurrentLogLevel; } }

        public static readonly ConsoleColor TimestampColor = ConsoleColor.Cyan;
        public static readonly ConsoleColor InfoColor = ConsoleColor.White;
        public static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        public static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
        public static readonly ConsoleColor ErrorColor = ConsoleColor.Red;

        public static void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = TimestampColor;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }
        public static void ClearLogs()
        {
            Console.Clear();

            DisplayHeader();
            DisplayInstructions();
        }
        public static void ToggleLoggingLevel()
        {
            _CurrentLogLevel = _CurrentLogLevel == LogLevel.Detailed ? LogLevel.Minimal : LogLevel.Detailed;
            Log($"Logging level set to {_CurrentLogLevel}", InfoColor);
        }

        public static void DisplayHeader()
        {
            Console.Clear();
            Log(@"
  ______ _                        _   ______              _                  
 |  ____| |                      | | |  ____|            | |                 
 | |__  | |_ ___ _ __ _ __   __ _| | | |__ _ __ __ _  ___| |_ _   _ _ __ ___ 
 |  __| | __/ _ \ '__| '_ \ / _` | | |  __| '__/ _` |/ __| __| | | | '__/ _ \
 | |____| ||  __/ |  | | | | (_| | | | |  | | | (_| | (__| |_| |_| | | |  __/
 |______|\__\___|_|  |_| |_|\__,_|_| |_|  |_|  \__,_|\___|\__|\__,_|_|  \___| ", TimestampColor);

            Console.WriteLine();
        }

        public static void DisplayInstructions()
        {
            Log("Press 'D' to disable all network interfaces.", InfoColor);
            Log("Press 'E' to enable all network interfaces.", InfoColor);
            Log($"Press 'L' to toggle logging level (Current: {_CurrentLogLevel}).", InfoColor);
            Log("Press 'Q' to quit.", InfoColor);
            Log("Press 'T' to toggle input mode between Global and Local inputs.", InfoColor);
            Log("Press 'S' to set temporary disable duration.", InfoColor);
            Log("Press 'R' to temporarily disable the network for the set duration.", InfoColor);
            Log("Press 'C' to clear console output.", InfoColor);
            Console.WriteLine();
        }

        public static void SelectInputMethod()
        {
            Log("Select Input Mode:", WarningColor);
            Log("1. Global Input", InfoColor);
            Log("2. Local Input", InfoColor);
            Log("Enter Your Choice (1/2): ", WarningColor);

            var choice = Console.ReadKey(true).KeyChar;

            InputHandler.UseAsyncKeyState = choice switch
            {
                '1' => true,
                '2' => false,
                _ => throw new ArgumentException("Invalid choice")
            };

            Console.WriteLine();
            Log($"Input mode set to: {(InputHandler.UseAsyncKeyState ? "Global Key Detection" : "Application-Specific Key Input")}", InfoColor);
            Thread.Sleep(300);
        }
        /*public static void SelectNetworkDisableMethod()
        {
            Log("Select Which Method You Want To Use:", WarningColor);
            Log("1. Windows Management Instrumentation", InfoColor);
            Log("2. NetSH Advanced Firewall (Requires Application Path)", InfoColor);
            Log("3. ", InfoColor);
            Log("Enter Your Choice (1/2): ", WarningColor);
        }*/

        public enum LogLevel
        {
            Minimal,
            Detailed
        }
    }
}
