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
using RunCat365.Properties;

namespace RunCat365
{
    struct GPUInfo
    {
        internal float Average { get; set; }
        internal float Maximum { get; set; }
    }

    internal static class GPUInfoExtension
    {
        internal static string GetDescription(this GPUInfo gpuInfo)
        {
            return $"{Strings.SystemInfo_GPU}: {gpuInfo.Maximum:f1}%";
        }

        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"{Strings.SystemInfo_GPU}:"),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Average}: {gpuInfo.Average:f1}%", false),
                TreeFormatter.CreateNode($"{Strings.SystemInfo_Maximum}: {gpuInfo.Maximum:f1}%", true)
            };
            return resultLines;
        }
    }

    internal class GPURepository
    {
        private readonly List<PerformanceCounter> gpuCounters = [];
        private readonly List<GPUInfo> gpuInfoList = [];
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;

        internal bool IsAvailable { get; private set; } = true;

        internal GPURepository()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();
                var instances = instanceNames.Where(n => n.Contains("engtype_3D")).ToList();
                if (instances.Count > 0)
                {
                    foreach (var instance in instances)
                    {
                        var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                        gpuCounters.Add(counter);

                        // Discards first return value
                        _ = counter.NextValue();
                    }
                }
                else
                {
                    IsAvailable = false;
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal void Update()
        {
            if (!IsAvailable || gpuCounters.Count == 0) return;
            try
            {
                var values = gpuCounters.Select(counter => counter.NextValue()).ToList();
                var average = values.Count > 0 ? values.Average() : 0f;
                var maximum = values.Count > 0 ? values.Max() : 0f;

                var gpuInfo = new GPUInfo
                {
                    Average = Math.Min(100, average),
                    Maximum = Math.Min(100, maximum)
                };

                gpuInfoList.Add(gpuInfo);
                if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
                {
                    gpuInfoList.RemoveAt(0);
                }
            }
            catch
            {
                IsAvailable = false;
            }
        }

        internal GPUInfo? Get()
        {
            if (!IsAvailable || gpuInfoList.Count == 0) return null;

            return new GPUInfo
            {
                Average = gpuInfoList.Average(x => x.Average),
                Maximum = gpuInfoList.Max(x => x.Maximum)
            };
        }

        internal void Close()
        {
            foreach (var counter in gpuCounters)
            {
                counter.Close();
            }
        }
    }
}
