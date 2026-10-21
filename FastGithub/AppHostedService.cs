using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FastGithub
{
    /// <summary>
    /// app后台服务
    /// </summary>
    sealed class AppHostedService : BackgroundService
    {
        private readonly IHostApplicationLifetime lifetime;
        private readonly IOptions<AppOptions> appOptions;
        private readonly IOptions<FastGithubOptions> fastGithubOptions;
        private readonly ILogger<AppHostedService> logger;

        public AppHostedService(
            IHostApplicationLifetime lifetime,
            IOptions<AppOptions> appOptions,
            IOptions<FastGithubOptions> fastGithubOptions,
            ILogger<AppHostedService> logger)
        {
            this.lifetime = lifetime;
            this.appOptions = appOptions;
            this.fastGithubOptions = fastGithubOptions;
            this.logger = logger;
        }

        /// <summary>
        /// 启动完成
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task StartAsync(CancellationToken cancellationToken)
        {
            Version version;
            if (ProductionVersion.Current is not null)
            {
                version = ProductionVersion.Current.Version;
            }
            else
            {
                version = new Version(0, 0);
            }
            this.logger.LogInformation($"\n======[ {nameof(FastGithub)} 启动完成，当前版本为 V{version} ]======\n");
            return base.StartAsync(cancellationToken);
        }

        /// <summary>
        /// 后台任务
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Delay(TimeSpan.FromSeconds(1d), stoppingToken);
            var uiExit = this.WaitForUiExitAsync(stoppingToken);
            await this.CheckFastGithubProxyAsync(stoppingToken);
            await uiExit;
        }


        /// <summary>
        /// 检测fastgithub代理设置
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task CheckFastGithubProxyAsync(CancellationToken cancellationToken)
        {
            if (OperatingSystem.IsWindows() == false || this.fastGithubOptions.Value.EnablePacketIntercept == false)
            {
                try
                {
                    if (await this.UseFastGithubProxyAsync() == false)
                    {
                        var httpProxyPort = this.fastGithubOptions.Value.HttpProxyPort;
                        this.logger.LogWarning($"请设置系统自动代理为http://{IPAddress.Loopback}:{httpProxyPort}，或手动代理http/https为{IPAddress.Loopback}:{httpProxyPort}");
                    }
                }
                catch (Exception)
                {
                    this.logger.LogWarning("尝试获取代理信息失败");
                }
            }
        }


        /// <summary>
        /// 应用fastgithub代理
        /// </summary>
        /// <param name="proxyServer"></param>
        /// <param name="httpProxyPort"></param>
        /// <returns></returns>
        private async Task<bool> UseFastGithubProxyAsync()
        {
            var systemProxy = HttpClient.DefaultProxy;
            if (systemProxy == null)
            {
                return false;
            }

            var domain = this.fastGithubOptions.Value.DomainConfigs.Keys.FirstOrDefault();
            if (domain == null)
            {
                return true;
            }

            var destination = new Uri($"https://{domain.Replace('*', 'a')}");
            var proxyServer = systemProxy.GetProxy(destination);
            if (proxyServer == null)
            {
                return false;
            }

            var httpProxyPort = this.fastGithubOptions.Value.HttpProxyPort;
            if (proxyServer.Port != httpProxyPort)
            {
                return false;
            }

            if (IPAddress.TryParse(proxyServer.Host, out var address))
            {
                return IPAddress.IsLoopback(address);
            }

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(proxyServer.Host);
                return addresses.Any(item => IPAddress.IsLoopback(item));
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// 等待父进程退出
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task WaitForUiExitAsync(CancellationToken cancellationToken)
        {
            var parentId = this.appOptions.Value.ParentProcessId;
            if (parentId <= 0)
            {
                return;
            }

            try
            {
                using var parent = Process.GetProcessById(parentId);
                using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                var parentExited = parent.WaitForExitAsync(linked.Token);
                var name = this.appOptions.Value.ShutdownEventName;
                if (OperatingSystem.IsWindows() && !string.IsNullOrEmpty(name))
                {
                    var signal = WaitForShutdownSignalAsync(name, linked.Token);
                    var completed = await Task.WhenAny(parentExited, signal);
                    await completed;
                }
                else await parentExited;
                linked.Cancel();
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                this.logger.LogError(ex, $"获取进程{parentId}异常");
            }
            finally
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    this.logger.LogInformation("界面请求停止或父进程已退出，正在正常关闭服务");
                    this.lifetime.StopApplication();
                }
            }
        }

        [System.Runtime.Versioning.SupportedOSPlatform("windows")]
        private static async Task WaitForShutdownSignalAsync(string name, CancellationToken cancellationToken)
        {
            using var signal = EventWaitHandle.OpenExisting(name);
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var cancellation = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
            var registered = ThreadPool.RegisterWaitForSingleObject(signal,
                (_, _) => completion.TrySetResult(true), null, Timeout.Infinite, executeOnlyOnce: true);
            try { await completion.Task; }
            finally { registered.Unregister(null); }
        }

    }
}
