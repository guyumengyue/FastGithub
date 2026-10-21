using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub.UI
{
    internal sealed class CoreProcessController : IDisposable
    {
        private Process? process;
        private EventWaitHandle? shutdownEvent;
        public bool IsRunning => this.process != null && !this.process.HasExited;
        public Uri? StatisticsUri { get; private set; }

        public void Start(bool enablePacketIntercept)
        {
            if (this.IsRunning) return;
            this.Dispose();
            var directory = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(directory, "fastgithub.exe");
            if (!File.Exists(path))
                throw new FileNotFoundException("未找到 fastgithub.exe。请将界面与核心程序放在同一发布目录。", path);
            var reservation = new TcpListener(IPAddress.Loopback, 0);
            reservation.Start();
            var port = ((IPEndPoint)reservation.LocalEndpoint).Port;
            reservation.Stop();
            var name = @"Local\FastGithub.UI.Stop." + Guid.NewGuid().ToString("N");
            this.shutdownEvent = new EventWaitHandle(false, EventResetMode.ManualReset, name);
            try
            {
                this.process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    WorkingDirectory = directory,
                    Arguments = $"ParentProcessId={Process.GetCurrentProcess().Id} UdpLoggerPort={UdpLogger.Port} UiPort={port} ShutdownEventName={name} FastGithub:EnablePacketIntercept={enablePacketIntercept.ToString().ToLowerInvariant()}",
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                if (this.process == null) throw new InvalidOperationException("核心进程未能启动，请查看日志。");
                this.StatisticsUri = new Uri($"http://127.0.0.1:{port}/flowStatistics");
            }
            catch { this.Dispose(); throw; }
        }

        public async Task StopAsync()
        {
            var running = this.process;
            if (running == null || running.HasExited) return;
            this.shutdownEvent!.Set();
            if (!await Task.Run(() => running.WaitForExit(12000)))
                throw new TimeoutException("服务仍在退出，请稍后重试。可在日志中查看停止进度。");
            this.StatisticsUri = null;
        }

        public void RequestStop() => this.shutdownEvent?.Set();
        public void Dispose()
        {
            this.process?.Dispose();
            this.process = null;
            this.shutdownEvent?.Dispose();
            this.shutdownEvent = null;
            this.StatisticsUri = null;
        }
    }
}
