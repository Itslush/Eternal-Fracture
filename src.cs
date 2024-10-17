using System.Management;

namespace NetworkToggle
{
    class Program
    {
        private static readonly Dictionary<string, NetworkInterface> NetworkInterfaces = new();
        private static LogLevel _currentLogLevel = LogLevel.Minimal;

        enum LogLevel
        {
            Minimal,
            Detailed
        }

        private class NetworkInterface
        {
            public string DeviceID { get; set; }
            public string Name { get; set; }
        }

        static void Main(string[] args)
        {
            DisplayHeader();
            DisplayInstructions();

            while (true)
            {
                var key = Console.ReadKey(true).Key;
                switch (key)
                {
                    case ConsoleKey.D:
                        DisableAllNetworkInterfaces();
                        break;
                    case ConsoleKey.E:
                        EnableAllNetworkInterfaces();
                        break;
                    case ConsoleKey.L:
                        ToggleLoggingLevel();
                        break;
                    case ConsoleKey.Q:
                        Log("Exiting...", ConsoleColor.Red);
                        return;
                }
            }
        }

        private static void DisplayHeader()
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("===========================================");
            Console.WriteLine("              Lag switch real              ");
            Console.WriteLine("===========================================");
            Console.ResetColor();
        }

        private static void DisplayInstructions()
        {
            Log("Press 'D' to disable all network interfaces.", ConsoleColor.DarkRed);
            Log("Press 'E' to enable all network interfaces.", ConsoleColor.DarkRed);
            Log($"Press 'L' to toggle logging level (Current: {_currentLogLevel}).", ConsoleColor.DarkRed);
            Log("Press 'Q' to quit.", ConsoleColor.DarkRed);
            Console.WriteLine();
        }

        private static void ToggleLoggingLevel()
        {
            _currentLogLevel = _currentLogLevel == LogLevel.Detailed ? LogLevel.Minimal : LogLevel.Detailed;
            Log($"[{GetTimestamp()}] -- Log level set to: {_currentLogLevel}", ConsoleColor.Green);
        }

        private static void DisableAllNetworkInterfaces()
        {
            RefreshNetworkInterfaces();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(netInterface.DeviceID);
                if (state == "Enabled" || state == "Connected")
                {
                    DisableNetwork(netInterface);
                }
                else if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"[{GetTimestamp()}] -- NetInterface || '{netInterface.Name}' already disabled.", ConsoleColor.Gray);
                }
            }
        }

        private static void EnableAllNetworkInterfaces()
        {
            RefreshNetworkInterfaces();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(netInterface.DeviceID);
                if (state == "Disabled" || state == "Disconnected")
                {
                    EnableNetwork(netInterface);
                }
                else if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"[{GetTimestamp()}] -- NetInterface || '{netInterface.Name}' already enabled or connected.", ConsoleColor.Gray);
                }
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
                    DeviceID = obj["DeviceID"].ToString(),
                    Name = obj["Name"].ToString()
                };
                NetworkInterfaces[netInterface.DeviceID] = netInterface;

                LogWithGradient($"Found Network Interface: {netInterface.Name}");
            }
        }

        private static string GetInterfaceState(string deviceID)
        {
            try
            {
                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{deviceID}'");
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
                    Log($"[{GetTimestamp()}] -- Error retrieving state for interface '{deviceID}': {ex.Message}", ConsoleColor.Red);
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
                    Log($"[{GetTimestamp()}] Enabling interface with DeviceID '{netInterface.DeviceID}'...", ConsoleColor.Green);
                }

                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{netInterface.DeviceID}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var result = obj.InvokeMethod("Enable", null);
                    if (_currentLogLevel == LogLevel.Detailed)
                    {
                        Log($"[{GetTimestamp()}] -- Enable command issued for '{netInterface.DeviceID}'. Result: {result}", ConsoleColor.Gray);
                    }

                    var state = GetInterfaceState(netInterface.DeviceID);
                    if (state == "Enabled")
                    {
                        Log($"[{GetTimestamp()}] -- Interface '{netInterface.Name}' enabled.", ConsoleColor.Green);
                    }
                    else
                    {
                        Log($"[{GetTimestamp()}] -- Failed to enable '{netInterface.Name}'. Current state: {state}", ConsoleColor.Red);
                        MonitorInterface(netInterface);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"[{GetTimestamp()}] -- Error enabling interface '{netInterface.DeviceID}': {ex.Message}", ConsoleColor.Red);
                }
                else
                {
                    Log($"[{GetTimestamp()}] -- Enable command for '{netInterface.Name}' failed.", ConsoleColor.Red);
                }
            }
        }

        private static void DisableNetwork(NetworkInterface netInterface)
        {
            try
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"[{GetTimestamp()}] -- Disabling interface with DeviceID '{netInterface.DeviceID}'...", ConsoleColor.Green);
                }

                var searcher = new ManagementObjectSearcher($"SELECT * FROM Win32_NetworkAdapter WHERE DeviceID = '{netInterface.DeviceID}'");
                foreach (ManagementObject obj in searcher.Get())
                {
                    var result = obj.InvokeMethod("Disable", null);
                    if (_currentLogLevel == LogLevel.Detailed)
                    {
                        Log($"[{GetTimestamp()}] -- Disable command issued for '{netInterface.DeviceID}'. Result: {result}", ConsoleColor.Gray);
                    }

                    var state = GetInterfaceState(netInterface.DeviceID);
                    if (state == "Disabled")
                    {
                        Log($"[{GetTimestamp()}] -- Interface '{netInterface.Name}' disabled.", ConsoleColor.Green);
                    }
                    else
                    {
                        Log($"[{GetTimestamp()}] -- Failed to disable '{netInterface.Name}'. Current state: {state}", ConsoleColor.Red);
                    }
                }
            }
            catch (Exception ex)
            {
                if (_currentLogLevel == LogLevel.Detailed)
                {
                    Log($"[{GetTimestamp()}] -- Error disabling interface '{netInterface.DeviceID}': {ex.Message}", ConsoleColor.Red);
                }
                else
                {
                    Log($"[{GetTimestamp()}] -- Disable command for '{netInterface.Name}' failed.", ConsoleColor.Red);
                }
            }
        }

        private static void MonitorInterface(NetworkInterface netInterface)
        {
            int waitTime = new Random().Next(5000, 10000);
            DateTime endTime = DateTime.Now.AddMilliseconds(waitTime);

            while (DateTime.Now < endTime)
            {
                Thread.Sleep(1000);
                var state = GetInterfaceState(netInterface.DeviceID);
                if (state == "Enabled")
                {
                    Log($"[{GetTimestamp()}] -- Interface '{netInterface.Name}' has been enabled after initial failure.");
                    return;
                }
            }
            Log($"[{GetTimestamp()}] -- Interface '{netInterface.Name}' remained disabled after monitoring period.");
        }

        private static void Log(string message, ConsoleColor color = ConsoleColor.White)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        private static void LogWithGradient(string message)
        {
            foreach (char c in message)
            {
                Console.ForegroundColor = InterpolateColor(ConsoleColor.Red, ConsoleColor.DarkRed, (double)message.IndexOf(c) / message.Length);
                Console.Write(c);
            }
            Console.ResetColor();
            Console.WriteLine();
        }

        private static string GetTimestamp()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        private static ConsoleColor InterpolateColor(ConsoleColor startColor, ConsoleColor endColor, double factor)
        {
            (int startR, int startG, int startB) = GetRGB(startColor);
            (int endR, int endG, int endB) = GetRGB(endColor);

            int r = (int)(startR + (endR - startR) * factor);
            int g = (int)(startG + (endG - startG) * factor);
            int b = (int)(startB + (endB - startB) * factor);

            return ClosestConsoleColor(r, g, b);
        }

        private static (int, int, int) GetRGB(ConsoleColor color)
        {
            return color switch
            {
                ConsoleColor.Black => (0, 0, 0),
                ConsoleColor.DarkBlue => (0, 0, 128),
                ConsoleColor.DarkGreen => (0, 128, 0),
                ConsoleColor.DarkCyan => (0, 128, 128),
                ConsoleColor.DarkRed => (128, 0, 0),
                ConsoleColor.DarkMagenta => (128, 0, 128),
                ConsoleColor.DarkYellow => (128, 128, 0),
                ConsoleColor.Gray => (192, 192, 192),
                ConsoleColor.DarkGray => (128, 128, 128),
                ConsoleColor.Blue => (0, 0, 255),
                ConsoleColor.Green => (0, 255, 0),
                ConsoleColor.Cyan => (0, 255, 255),
                ConsoleColor.Red => (255, 0, 0),
                ConsoleColor.Magenta => (255, 0, 255),
                ConsoleColor.Yellow => (255, 255, 0),
                ConsoleColor.White => (255, 255, 255),
                _ => (255, 255, 255)
            };
        }

        private static ConsoleColor ClosestConsoleColor(int r, int g, int b)
        {
            var colors = Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().ToArray();
            ConsoleColor closestColor = ConsoleColor.White;
            double closestDistance = double.MaxValue;

            foreach (var color in colors)
            {
                var (cr, cg, cb) = GetRGB(color);
                double distance = Math.Sqrt(Math.Pow(cr - r, 2) + Math.Pow(cg - g, 2) + Math.Pow(cb - b, 2));

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestColor = color;
                }
            }

            return closestColor;
        }
    }
}
