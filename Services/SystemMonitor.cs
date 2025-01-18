using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EBDC.Models;
using EBDC.Services.Interfaces;
using Timer = System.Threading.Timer;

namespace EBDC.Services
{
    public class SystemMonitor : ISystemMonitor
    {
        private readonly Process _currentProcess;
        private readonly Timer _timer;
        private bool _disposed;

        public event Action<ResourcesInfo>? ResourcesUpdated;

        public SystemMonitor()
        {
            _currentProcess = Process.GetCurrentProcess();
            _timer = new Timer(UpdateResourceUsage, null, Timeout.Infinite, Timeout.Infinite);
        }

        public void Start()
        {
            _timer.Change(0, 5000); // Aggiorna ogni 5 secondi
        }

        private void UpdateResourceUsage(object? state)
        {
            if (_disposed) return;

            try
            {
                _currentProcess.Refresh();
                var memoryMB = _currentProcess.WorkingSet64 / (1024.0 * 1024.0);

                var info = new ResourcesInfo
                {
                    MemoryUsageMB = memoryMB
                };

                ResourcesUpdated?.Invoke(info);
            }
            catch (Exception)
            {
                // Ignora eventuali errori durante il monitoraggio
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _timer.Dispose();
            _currentProcess.Dispose();
        }
    }
}
