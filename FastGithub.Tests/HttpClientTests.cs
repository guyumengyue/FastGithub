using FastGithub.Configuration;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Xunit;

namespace FastGithub.Tests;

public class HttpClientTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CustomSniMustNotBypassUntrustedCertificateChains(bool ignoreNameMismatch)
    {
        using var rsa = RSA.Create(2048);
        var certificateRequest = new CertificateRequest("CN=example.test", rsa,
            HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var names = new SubjectAlternativeNameBuilder();
        names.AddDnsName("example.test");
        certificateRequest.CertificateExtensions.Add(names.Build());
        using var certificate = certificateRequest.CreateSelfSigned(DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddDays(1));
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var listenerSocket = listener.Server;
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var server = ServeTlsAsync();

        using var client = new Http.HttpClient(new DomainConfig
        {
            TlsSni = true,
            TlsSniPattern = "different.test",
            TlsIgnoreNameMismatch = ignoreNameMismatch
        }, new TestDomainResolver());
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://example.test:{port}/");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, timeout.Token));
        await server;

        async Task ServeTlsAsync()
        {
            using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
            using var stream = new SslStream(connection.GetStream());
            try
            {
                await stream.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                {
                    ServerCertificate = certificate,
                    EnabledSslProtocols = SslProtocols.Tls12
                }, timeout.Token);
                var buffer = new byte[4096];
                await stream.ReadAsync(buffer, timeout.Token);
                await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), timeout.Token);
            }
            catch (Exception ex) when (ex is AuthenticationException || ex is IOException)
            {
                // The client is expected to reject this untrusted certificate.
            }
        }
    }

    [Fact]
    public async Task PreservesNonDefaultPortInHostHeader()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var listenerSocket = listener.Server;
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var server = RespondAsync();

        using var client = new Http.HttpClient(new DomainConfig(), new TestDomainResolver());
        using var request = new HttpRequestMessage(HttpMethod.Get, $"http://example.test:{port}/");
        using var response = await client.SendAsync(request, timeout.Token);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains($"Host: example.test:{port}\r\n", await server);

        async Task<string> RespondAsync()
        {
            using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
            using var stream = connection.GetStream();
            var buffer = new byte[4096];
            var headers = new StringBuilder();
            while (!headers.ToString().Contains("\r\n\r\n"))
            {
                var count = await stream.ReadAsync(buffer, timeout.Token);
                Assert.True(count > 0);
                headers.Append(Encoding.ASCII.GetString(buffer, 0, count));
            }
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Length: 0\r\nConnection: close\r\n\r\n"), timeout.Token);
            return headers.ToString();
        }
    }

    [Fact]
    public async Task FailedTlsHandshakeClosesUnderlyingConnection()
    {
        var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        using var listenerSocket = listener.Server;
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var server = RejectTlsAsync();

        using var client = new Http.HttpClient(new DomainConfig { TlsSni = true }, new TestDomainResolver());
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://example.test:{port}/");
        await Assert.ThrowsAsync<HttpRequestException>(() => client.SendAsync(request, timeout.Token));
        Assert.Equal(0, await server);

        async Task<int> RejectTlsAsync()
        {
            using var connection = await listener.AcceptTcpClientAsync(timeout.Token);
            using var stream = connection.GetStream();
            var buffer = new byte[16384];
            Assert.True(await stream.ReadAsync(buffer, timeout.Token) > 0);
            await stream.WriteAsync(Encoding.ASCII.GetBytes("HTTP/1.1 400 Bad Request\r\n\r\n"), timeout.Token);
            // A leaked SslStream leaves the server waiting until this deadline expires.
            return await stream.ReadAsync(buffer, timeout.Token);
        }
    }
}
