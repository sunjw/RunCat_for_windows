// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Linq;

namespace RunCat365
{
    struct NetworkInfo
    {
        internal float SentSpeed { get; set; }
        internal float ReceivedSpeed { get; set; }
    }

    internal static class NetworkInfoExtension
    {
        internal static string GetDescription(this NetworkInfo networkInfo)
        {
            return $"Sent: {NetworkUtils.FormatSpeed(networkInfo.SentSpeed)}\nReceived: {NetworkUtils.FormatSpeed(networkInfo.ReceivedSpeed)}";
        }

        internal static List<string> GenerateIndicator(this NetworkInfo networkInfo)
        {
            return new List<string>
            {
                $"Network:",
                $"   ├─ Sent: {NetworkUtils.FormatSpeed(networkInfo.SentSpeed)}",
                $"   └─ Received: {NetworkUtils.FormatSpeed(networkInfo.ReceivedSpeed)}"
            };
        }
    }

    internal class NetworkRepository
    {
        private readonly PerformanceCounter netSentSpeed;
        private readonly PerformanceCounter netReceivedSpeed;
        private readonly string networkInterfaceName;
        private NetworkInfo networkInfo;

        internal NetworkRepository()
        {
            networkInterfaceName = NetworkUtils.GetNetworkInterfaceName() ?? throw new InvalidOperationException("No valid network interface found.");
            netSentSpeed = new PerformanceCounter("Network Interface", "Bytes Sent/sec", networkInterfaceName);
            netReceivedSpeed = new PerformanceCounter("Network Interface", "Bytes Received/sec", networkInterfaceName);
            // Discard first value
            _ = netSentSpeed.NextValue();
            _ = netReceivedSpeed.NextValue();
        }

        internal void Update()
        {
            networkInfo.SentSpeed = netSentSpeed.NextValue();
            networkInfo.ReceivedSpeed = netReceivedSpeed.NextValue();
        }

        internal NetworkInfo Get()
        {
            Update();
            return networkInfo;
        }

        internal void Close()
        {
            netSentSpeed.Close();
            netReceivedSpeed.Close();
        }
    }

    internal static class NetworkUtils
    {
        internal static string FormatSpeed(float speedBytes)
        {
            return ((long)speedBytes).ToByteFormatted() + "/s";
        }

        internal static string? GetNetworkInterfaceName()
        {
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces.FirstOrDefault(ni =>
                ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Tunnel &&
                ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                !ni.Description.ToLower().Contains("vpn") &&
                !ni.Description.ToLower().Contains("tap") &&
                !ni.Description.ToLower().Contains("virtual") &&
                !ni.Description.ToLower().Contains("tun"));
            return networkInterface?.Description;
        }
    }
}