using FastGithub.DomainResolve;
using FastGithub.HttpServer.Certs;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Xunit;

// CertService uses paths relative to the working directory. Keep its temporary
// CA isolated and disable installers so these tests never change OS trust.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace FastGithub.Tests;

public class ProxyModeTests
{
    [Fact]
    public async Task ExplicitModeServesPacWithoutRegisteringPacketInterceptors()
    {
        var previousDirectory = Environment.CurrentDirectory;
        var directory = Path.Combine(Path.GetTempPath(), $"fastgithub-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        try
        {
            Environment.CurrentDirectory = directory;
            var portReservation = new TcpListener(IPAddress.Loopback, 0);
            portReservation.Start();
            var port = ((IPEndPoint)portReservation.LocalEndpoint).Port;
            portReservation.Stop();
            var uiReservation = new TcpListener(IPAddress.Loopback, 0);
            uiReservation.Start();
            var uiPort = ((IPEndPoint)uiReservation.LocalEndpoint).Port;
            uiReservation.Stop();

            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { ContentRootPath = directory });
            builder.Configuration.Sources.Clear();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["FastGithub:EnablePacketIntercept"] = "false",
                ["UiPort"] = uiPort.ToString(),
                ["FastGithub:HttpProxyPort"] = port.ToString(),
                ["FastGithub:DomainConfigs:github.com:TlsSni"] = "true"
            });
            builder.ConfigureWebHost();
            builder.ConfigureServices();
            Assert.DoesNotContain(builder.Services, service =>
                service.ImplementationType?.Namespace == "FastGithub.PacketIntercept");

            // Exercise the real HTTP listener without starting external DNS processes.
            foreach (var service in builder.Services.Where(service =>
                service.ServiceType == typeof(IHostedService) &&
                service.ImplementationType?.Namespace?.StartsWith("FastGithub") == true).ToArray())
            {
                builder.Services.Remove(service);
            }
            builder.Services.RemoveAll<ICaCertInstaller>();
            builder.Services.AddSingleton<IDomainResolver, TestDomainResolver>();

            await using var app = builder.Build();
            app.ConfigureApp();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            await app.StartAsync(timeout.Token);
            try
            {
                using var client = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler { UseProxy = false });
                using var response = await client.GetAsync($"http://127.0.0.1:{port}/", timeout.Token);
                var pac = await response.Content.ReadAsStringAsync(timeout.Token);
                Assert.Equal("application/x-ns-proxy-autoconfig", response.Content.Headers.ContentType?.MediaType);
                Assert.Contains($"PROXY 127.0.0.1:{port}", pac);
                Assert.Contains("github.com", pac);
                var addresses = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses;
                Assert.All(addresses, address => Assert.True(address.EndsWith($":{port}") || address.EndsWith($":{uiPort}")));
                using var statistics = await client.GetAsync($"http://127.0.0.1:{uiPort}/flowStatistics", timeout.Token);
                Assert.Equal("application/json", statistics.Content.Headers.ContentType?.MediaType);
                Assert.Contains("TotalRead", await statistics.Content.ReadAsStringAsync(timeout.Token));
                using var invalidUiRequest = await client.GetAsync($"http://127.0.0.1:{uiPort}/other", timeout.Token);
                Assert.Equal(HttpStatusCode.NotFound, invalidUiRequest.StatusCode);

                var upstream = new TcpListener(IPAddress.Loopback, 0);
                upstream.Start();
                using var upstreamSocket = upstream.Server;
                var upstreamPort = ((IPEndPoint)upstream.LocalEndpoint).Port;
                var server = RespondAsync();
                using var proxyClient = new System.Net.Http.HttpClient(new System.Net.Http.HttpClientHandler
                {
                    Proxy = new WebProxy($"http://127.0.0.1:{port}")
                });
                var body = await proxyClient.GetStringAsync($"http://github.com:{upstreamPort}/", timeout.Token);
                Assert.Equal("proxied", body);
                Assert.Contains($"Host: github.com:{upstreamPort}\r\n", await server);

                async Task<string> RespondAsync()
                {
                    using var connection = await upstream.AcceptTcpClientAsync(timeout.Token);
                    using var stream = connection.GetStream();
                    var buffer = new byte[4096];
                    var headers = new StringBuilder();
                    while (!headers.ToString().Contains("\r\n\r\n"))
                    {
                        var count = await stream.ReadAsync(buffer, timeout.Token);
                        Assert.True(count > 0);
                        headers.Append(Encoding.ASCII.GetString(buffer, 0, count));
                    }
                    await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 7\r\nConnection: close\r\n\r\nproxied"), timeout.Token);
                    return headers.ToString();
                }
            }
            finally
            {
                await app.StopAsync(timeout.Token);
            }
        }
        finally
        {
            Environment.CurrentDirectory = previousDirectory;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void TransparentModeRemainsTheDefaultOnWindows()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Configuration.Sources.Clear();
        builder.ConfigureServices();
        var hasInterceptors = builder.Services.Any(service =>
            service.ImplementationType?.Namespace == "FastGithub.PacketIntercept");
        Assert.Equal(OperatingSystem.IsWindows(), hasInterceptors);
    }
}
