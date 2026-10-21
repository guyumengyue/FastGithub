using FastGithub.DomainResolve;
using System.Net;
using System.Runtime.CompilerServices;

namespace FastGithub.Tests;

sealed class TestDomainResolver : IDomainResolver
{
    public async IAsyncEnumerable<IPAddress> ResolveAsync(DnsEndPoint endPoint,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        cancellationToken.ThrowIfCancellationRequested();
        yield return IPAddress.Loopback;
    }

    public Task TestSpeedAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
