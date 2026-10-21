using FastGithub.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using Xunit;

namespace FastGithub.Tests;

public class AppHostedServiceControlTests
{
    [Fact]
    public async Task UiStopSignalRequestsNormalHostShutdownWhileParentIsAlive()
    {
        if (!OperatingSystem.IsWindows()) return;
        var name = @"Local\FastGithub.Test.Stop." + Guid.NewGuid().ToString("N");
        using var signal = new EventWaitHandle(false, EventResetMode.ManualReset, name);
        var lifetime = new TestLifetime();
        using var service = new AppHostedService(lifetime,
            Options.Create(new AppOptions { ParentProcessId = Environment.ProcessId, ShutdownEventName = name }),
            Options.Create(new FastGithubOptions()), NullLogger<AppHostedService>.Instance);
        await service.StartAsync(CancellationToken.None);
        signal.Set();
        var completed = await Task.WhenAny(lifetime.Stopped.Task, Task.Delay(TimeSpan.FromSeconds(6)));
        Assert.Same(lifetime.Stopped.Task, completed);
        Assert.False(Process.GetCurrentProcess().HasExited);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        await service.StopAsync(deadline.Token);
    }

    private sealed class TestLifetime : IHostApplicationLifetime
    {
        public TaskCompletionSource<bool> Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ApplicationStarted => CancellationToken.None;
        public CancellationToken ApplicationStopping => CancellationToken.None;
        public CancellationToken ApplicationStopped => CancellationToken.None;
        public void StopApplication() => Stopped.TrySetResult(true);
    }
}
