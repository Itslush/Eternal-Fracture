using System.Management;
using System.Runtime.InteropServices;

namespace NetworkToggle
{
    class Initialize
    {
        private static readonly Dictionary<int, bool> KeyProcessed = new Dictionary<int, bool>() { { 0x44, false }, { 0x45, false }, { 0x4C, false }, { 0x51, false }, { 0x54, false } };
        private static readonly Dictionary<ConsoleKey, Action> KeyActions = new()
        {
             { ConsoleKey.D, () => DisableAllNetworkInterfaces(netInterface) }, { ConsoleKey.E, EnableAllNetworkInterfaces },
             { ConsoleKey.L, ToggleLoggingLevel }, { ConsoleKey.Q, () => { Log("Exiting...", ErrorColor); Environment.Exit(0); } },
             { ConsoleKey.T, ToggleInputMode }, { ConsoleKey.S, SetDisableDuration },
             { ConsoleKey.R, () => TempDisableNetInterfaces(disableDurationSeconds) }
        };
        private static readonly Dictionary<string, NetworkInterface> NetworkInterfaces = [];

        private static readonly string CurrentDateTime = DateTime.Now.ToString("HH:mm:ss");

        private static readonly TimeSpan ToggleCooldown = TimeSpan.FromMilliseconds(300);

        private static readonly ConsoleColor TimestampColor = ConsoleColor.Cyan;
        private static readonly ConsoleColor InfoColor = ConsoleColor.White;
        private static readonly ConsoleColor SuccessColor = ConsoleColor.Green;
        private static readonly ConsoleColor WarningColor = ConsoleColor.Yellow;
        private static readonly ConsoleColor ErrorColor = ConsoleColor.Red;
        private static readonly ConsoleColor LogLevelColor = ConsoleColor.DarkYellow;

        private static NetworkInterface? netInterface;
        private static DateTime _lastToggleTime = DateTime.MinValue;
        private static LogLevel _currentLogLevel = LogLevel.Detailed;

        private static bool _useAsyncKeyState = true;
        private static int disableDurationSeconds = 5;

        enum LogLevel
        {
            Minimal,
            Detailed
        }

        class NetworkInterface
        {
            public string? DeviceId { get; set; }
            public string? Name { get; set; }
        }

        private class NetworkInterfaceStatus
        {
            public string? Name { get; set; }
            public string? Status { get; set; }
        }

        [DllImport("User32.dll", EntryPoint = "GetAsyncKeyState")]
        private static extern short GetAsyncKeyState(int vKey);
        static void Main(string[] args)
        {
            SelectInputMethod();
            DisplayHeader();
            DisplayInstructions();

            while (true)
            {
                if (_useAsyncKeyState)
                {
                    CheckAsyncKeyState();
                }
                else
                {
                    CheckConsoleReadKey();
                }

                if (_useAsyncKeyState) Thread.Sleep(20);
            }
        }

        private static void SelectInputMethod()
        {
            Log("Select Input Mode:", WarningColor);
            Log("1. Global Input", InfoColor);
            Log("2. Local Input", InfoColor);
            Log("Enter your choice (1 or 2): ", WarningColor);

            var choice = Console.ReadKey(true).KeyChar;

            _useAsyncKeyState = choice switch
            {
                '1' => true,
                '2' => false,
                _ => throw new ArgumentException("Invalid choice")
            };

            Console.WriteLine("");
            Log($"Input mode set to: {(_useAsyncKeyState ? "Global Key Detection" : "Application-Specific Key Input")}", InfoColor);
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

        private static void CheckAsyncKeyState()
        {
            CheckKeyState(0x44, () => DisableAllNetworkInterfaces(netInterface));
            CheckKeyState(0x45, EnableAllNetworkInterfaces);
            CheckKeyState(0x4C, ToggleLoggingLevel);
            CheckKeyState(0x51, () =>
            {
                Log("Exiting...", ErrorColor);
                Environment.Exit(0);
            });
            CheckKeyState(0x54, ToggleInputMode);
            CheckKeyState(0x53, SetDisableDuration);
            CheckKeyState(0x52, () => TempDisableNetInterfaces(disableDurationSeconds));
        }

        private static void CheckConsoleReadKey()
        {
            var key = Console.ReadKey(true).Key;

            if (KeyActions.TryGetValue(key, out var action))
            {
                action();
            }
        }

        private static void ToggleInputMode()
        {
            if (DateTime.Now - _lastToggleTime < ToggleCooldown) return;

            _lastToggleTime = DateTime.Now;
            _useAsyncKeyState = !_useAsyncKeyState;
            Log($"Input mode toggled. Using {(_useAsyncKeyState ? "GetAsyncKeyState" : "Console.ReadKey")} for input.", InfoColor);
        }

        private static void DisplayHeader()
        {
            Console.Clear();
            Log(@"
    ______ _                        _    __                _                  
   |  ____| |                      | |  / _|              | |                 
   | |__  | |_ ___ _ __ _ __   __ _| | | |_ _ __ __ _  ___| |_ _   _ _ __ ___ 
   |  __| | __/ _ \ '__| '_ \ / _` | | |  _| '__/ _` |/ __| __| | | | '__/ _ \
   | |____| ||  __/ |  | | | | (_| | | | | | | | (_| | (__| |_| |_| | | |  __/
   |______|\__\___|_|  |_| |_|\__,_|_| |_| |_|  \__,_|\___|\__|\__,_|_|  \___|", ConsoleColor.Cyan);
            Console.WriteLine();
        }

        private static void DisplayInstructions()
        {
            Log("Press 'D' to disable all network interfaces.", InfoColor);
            Log("Press 'E' to enable all network interfaces.", InfoColor);
            Log($"Press 'L' to toggle logging level (Current: {_currentLogLevel}).", InfoColor);
            Log("Press 'Q' to quit.", InfoColor);
            Log("Press 'T' to toggle input mode between Global and Local inputs.", InfoColor);
            Log($"Press 'S' to set temporary disable duration (Current: {disableDurationSeconds} seconds).", InfoColor);
            Log("Press 'R' to temporarily disable the network for the set duration.", InfoColor);
            Console.WriteLine();
        }

        private static void ToggleLoggingLevel()
        {
            _currentLogLevel = _currentLogLevel == LogLevel.Detailed ? LogLevel.Minimal : LogLevel.Detailed;
            Log($"Log level set to: {_currentLogLevel}", SuccessColor);
        }

        private static void DisableAllNetworkInterfaces(NetworkInterface netInterface)
        {
            RefreshNetworkInterfaces();

            foreach (var currentInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(currentInterface.DeviceId);

                if (state == "Enabled" || state == "Connected")
                {
                    DisableNetwork(currentInterface);
                }
                else if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"NetInterface | '{currentInterface.Name}' already disabled.", ConsoleColor.Gray);
                }
            }

            DisplayInstructions();
        }

        private static void EnableAllNetworkInterfaces()
        {
            RefreshNetworkInterfaces();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(netInterface.DeviceId);

                if (state == "Disabled" || state == "Disconnected")
                {
                    EnableNetwork(netInterface);
                }
                else if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"NetInterface | '{netInterface.Name}' already enabled or connected.", ConsoleColor.Gray);
                }
            }

            DisplayInstructions();
        }

        private static void TempDisableNetInterfaces(int duration = -1)
        {
            if (duration == -1)
            {
                duration = new Random().Next(5, 11);
            }

            disableDurationSeconds = duration;

            var uniqueNetworkInterfaces = new HashSet<NetworkInterface>();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                uniqueNetworkInterfaces.Add(netInterface);
            }

            foreach (var netInterface in uniqueNetworkInterfaces)
            {
                var state = GetInterfaceState(netInterface.DeviceId);

                if (state == "Enabled" || state == "Connected")
                {
                    DisableNetwork(netInterface);
                }
                else if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"NetInterface | '{netInterface.Name}' already disabled.", ConsoleColor.Gray);
                }
            }

            Log($"Network interfaces disabled for {duration} seconds.", WarningColor);

            new Thread(() =>
            {
                Thread.Sleep(duration * 1000);
                EnableAllNetworkInterfaces();
            }).Start();
        }

        private static void SetDisableDuration()
        {
            Console.Write("Enter the number of seconds to temporarily disable the network (min 5 seconds): ");

            if (int.TryParse(Console.ReadLine(), out int seconds) && seconds >= 5)
            {
                disableDurationSeconds = seconds;
                Log($"Network will be disabled for {seconds} seconds when triggered.", InfoColor);
            }
            else
            {
                Log("Invalid input. Duration set to default 5-10 seconds.", ErrorColor);
            }
        }

        private static void RefreshNetworkInterfaces()
        {
            NetworkInterfaces.Clear();

            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_NetworkAdapter");

            foreach (ManagementObject obj in searcher.Get())
            {
                var netInterface = new NetworkInterface
                {
                    DeviceId = obj["DeviceID"].ToString(),
                    Name = obj["Name"].ToString()
                };

                NetworkInterfaces[netInterface.DeviceId] = netInterface;
            }

            var networkInterfaceStatuses = new List<NetworkInterfaceStatus>();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                var status = GetInterfaceState(netInterface.DeviceId);
                networkInterfaceStatuses.Add(new NetworkInterfaceStatus
                {
                    Name = netInterface.Name,
                    Status = status
                });
            }

            var groupedNetworkInterfaces = networkInterfaceStatuses.GroupBy(x => x.Status);

            Log($"{CurrentDateTime} Network Interface | Status", WarningColor);
            Log($"{CurrentDateTime} -----------------|-------", WarningColor);

            foreach (var group in groupedNetworkInterfaces)
            {
                var statusColor = group.Key == "Enabled" ? ConsoleColor.Green : ConsoleColor.Red;

                foreach (var netInterfaceStatus in group)
                {
                    Log($"{CurrentDateTime} {netInterfaceStatus.Name} | {netInterfaceStatus.Status}", statusColor);
                }

                Console.ResetColor();
            }
        }

        private static string GetInterfaceState(string deviceId)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{deviceId}'");
                var obj = searcher.Get().Cast<ManagementObject>().FirstOrDefault();

                if (obj != null)
                {
                    var isEnabledProp = obj.Properties.Cast<PropertyData>().FirstOrDefault(p => p.Name == "NetEnabled");

                    if (isEnabledProp?.Value is bool isEnabled)
                    {
                        return isEnabled ? "Enabled" : "Disabled";
                    }
                }
            }
            catch (Exception ex)
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"Error retrieving state for interface '{deviceId}': {ex.Message}", ErrorColor);
                }
            }

            return "Unknown";
        }

        private static void EnableNetwork(NetworkInterface netInterface)
        {
            try
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"Enabling interface with DeviceID '{netInterface.DeviceId}'...", SuccessColor);
                }

                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{netInterface.DeviceId}'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    obj.InvokeMethod("Enable", null);
                    Log($"Successfully enabled interface '{netInterface.Name}'.", SuccessColor);
                }
            }
            catch (Exception ex)
            {
                Log($"Error enabling interface '{netInterface.Name}': {ex.Message}", ErrorColor);
            }
        }

        private static void DisableNetwork(NetworkInterface netInterface)
        {
            try
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"Disabling interface with DeviceID '{netInterface.DeviceId}'...", SuccessColor);
                }

                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{netInterface.DeviceId}'");

                foreach (ManagementObject obj in searcher.Get())
                {
                    obj.InvokeMethod("Disable", null);
                    Log($"Successfully disabled interface '{netInterface.Name}'.", SuccessColor);
                }
            }
            catch (Exception ex)
            {
                Log($"Error disabling interface '{netInterface.Name}': {ex.Message}", ErrorColor);
            }
        }

        private static void Log(string message, ConsoleColor color)
        {
            Console.ForegroundColor = TimestampColor;
            Console.Write($"[{DateTime.Now:HH:mm:ss}] ");
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
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

        // -- didnt work out as i wanted it to :/  ^^^
    }
}
