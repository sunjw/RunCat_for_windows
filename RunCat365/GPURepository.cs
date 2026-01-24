using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RunCat365
{
    struct GPUInfo
    {
        internal float Total { get; set; }
    }

    internal static class GPUInfoExtension
    {
        internal static string GetDescription(this GPUInfo gpuInfo)
        {
            return $"GPU: {gpuInfo.Total:f1}%";
        }

        internal static List<string> GenerateIndicator(this GPUInfo gpuInfo)
        {
            var resultLines = new List<string>
            {
                TreeFormatter.CreateRoot($"GPU: {gpuInfo.Total:f1}%")
            };
            return resultLines;
        }
    }

    internal class GPURepository
    {
        private PerformanceCounter gpuCounter;
        private bool isGpuAvailable = true;
        private readonly List<GPUInfo> gpuInfoList = [];
        private const int GPU_INFO_LIST_LIMIT_SIZE = 5;

        internal bool IsAvailable => isGpuAvailable;

        internal GPURepository()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var instanceNames = category.GetInstanceNames();
                var instance = instanceNames.FirstOrDefault(n => n.Contains("engtype_3D"));
                if (instance != null)
                {
                    gpuCounter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance);
                    _ = gpuCounter.NextValue();
                }
                else
                {
                    isGpuAvailable = false;
                }
            }
            catch
            {
                isGpuAvailable = false;
            }
        }

        internal void Update()
        {
            if (!isGpuAvailable || gpuCounter == null) return;
            try
            {
                var value = Math.Min(100, gpuCounter.NextValue());
                var gpuInfo = new GPUInfo
                {
                    Total = value
                };
                gpuInfoList.Add(gpuInfo);
                if (GPU_INFO_LIST_LIMIT_SIZE < gpuInfoList.Count)
                {
                    gpuInfoList.RemoveAt(0);
                }
            }
            catch
            {
                isGpuAvailable = false;
            }
        }

        internal GPUInfo Get()
        {
            if (!isGpuAvailable || gpuInfoList.Count == 0)
            {
                return new GPUInfo();
            }
            return new GPUInfo
            {
                Total = gpuInfoList.Average(x => x.Total)
            };
        }

        internal void Close()
        {
            gpuCounter?.Close();
        }
    }
}
