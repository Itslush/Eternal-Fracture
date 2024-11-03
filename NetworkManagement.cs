using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using static EternalFracture.ConsoleLogging;

namespace EternalFracture
{
    class NetworkManagementWMI
    {
        private static readonly Dictionary<string, NetworkInterface> NetworkInterfaces = new();

        public static void DisableAllNetworkInterfaces()
        {
            RefreshNetworkInterfaces();

            foreach (var currentInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(currentInterface.DeviceId);

                if (state == "Enabled" || state == "Connected")
                {
                    DisableNetwork(currentInterface);
                }
                else if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
                {
                    Log($"NetInterface | '{currentInterface.Name}' already disabled.", ConsoleColor.Gray);
                }
            }

            DisplayInstructions();
        }

        public static void EnableAllNetworkInterfaces()
        {
            RefreshNetworkInterfaces();

            foreach (var netInterface in NetworkInterfaces.Values)
            {
                var state = GetInterfaceState(netInterface.DeviceId);

                if (state == "Disabled" || state == "Disconnected")
                {
                    EnableNetwork(netInterface);
                }
                else if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
                {
                    Log($"NetInterface | '{netInterface.Name}' already enabled or connected.", ConsoleColor.Gray);
                }
            }

            DisplayInstructions();
        }

        public static int disableDurationSeconds = 5;
        public static void TempDisableNetInterfaces(int duration = -1)
        {
            if (duration == -1)
            {
                duration = new Random().Next(5, 11);
            }

            disableDurationSeconds = duration;

            DisableAllNetworkInterfaces();
            Log($"Network interfaces disabled for {duration} seconds.", WarningColor);

            Thread.Sleep(duration * 1000);

            EnableAllNetworkInterfaces();
            Log("Network interfaces re-enabled.", SuccessColor);
        }

        public static void SetDisableDuration()
        {
            Log("Enter the number of seconds to temporarily disable the network (min 5 seconds): ", InfoColor);

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

            if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
            {
                var groupedNetworkInterfaces = networkInterfaceStatuses.GroupBy(x => x.Status);

                Log($" Network Interface | Status", WarningColor);
                Log($" ------------------|-------", WarningColor);

                foreach (var group in groupedNetworkInterfaces)
                {
                    var statusColor = group.Key == "Enabled" ? ConsoleColor.Green : ConsoleColor.Red;

                    foreach (var netInterfaceStatus in group)
                    {
                        Log($"{netInterfaceStatus.Name} | {netInterfaceStatus.Status}", statusColor);
                    }

                    Console.ResetColor();
                }
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
                if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
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
                if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
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
                if (ConsoleLogging.ReturnLogLevel == LogLevel.Detailed)
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
    }
    public class NetworkInterface
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
    }
    public class NetworkInterfaceStatus
    {
        public string? Name { get; set; }
        public string? Status { get; set; }
    }
}
