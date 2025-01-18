using System;
using EBDC.Models;

namespace EBDC.Services.Interfaces
{
    public interface ISystemMonitor : IDisposable
    {
        event Action<ResourcesInfo> ResourcesUpdated;
        void Start();
    }

    public record SystemResourceInfo(double CpuUsage, double RamUsage, double AvailableMemoryMB);
}
