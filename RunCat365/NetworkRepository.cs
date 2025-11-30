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

using System.Net.NetworkInformation;

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
        private readonly NetworkInterface networkInterface;
        private long lastSent;
        private long lastReceived;
        private DateTime lastUpdate;
        private NetworkInfo networkInfo;

        internal NetworkRepository()
        {
            networkInterface = NetworkUtils.GetNetworkInterface()
                ?? throw new InvalidOperationException("No valid network interface found.");
            var stats = networkInterface.GetIPStatistics();
            lastSent = stats.BytesSent;
            lastReceived = stats.BytesReceived;
            lastUpdate = DateTime.UtcNow;
        }

        internal void Update()
        {
            var stats = networkInterface.GetIPStatistics();
            var now = DateTime.UtcNow;
            var elapsedSec = (now - lastUpdate).TotalSeconds;
            if (elapsedSec > 0)
            {
                networkInfo.SentSpeed = (float)((stats.BytesSent - lastSent) / elapsedSec);
                networkInfo.ReceivedSpeed = (float)((stats.BytesReceived - lastReceived) / elapsedSec);
            }
            lastSent = stats.BytesSent;
            lastReceived = stats.BytesReceived;
            lastUpdate = now;
        }

        internal NetworkInfo Get()
        {
            Update();
            return networkInfo;
        }
    }

    internal static class NetworkUtils
    {
        internal static string FormatSpeed(float speedBytes)
        {
            return ((long)speedBytes).ToByteFormatted() + "/s";
        }

        internal static NetworkInterface? GetNetworkInterface()
        {
            var interfaces = NetworkInterface.GetAllNetworkInterfaces();
            var networkInterface = interfaces.FirstOrDefault(ni =>
                ni.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                ni.OperationalStatus == OperationalStatus.Up &&
                !ni.Description.ToLower().Contains("vpn") &&
                !ni.Description.ToLower().Contains("tap") &&
                !ni.Description.ToLower().Contains("virtual") &&
                !ni.Description.ToLower().Contains("tun"));
            return networkInterface;
        }
    }
}
