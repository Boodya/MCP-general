using System.IO.Pipelines;
using McpGenericServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace McpGenericServer.Tests;

public sealed class McpSdkIntegrationTests
{
    [Fact]
    public async Task ListTools_ReturnsSdkRegisteredTools()
    {
        await using var fixture = await SdkServerFixture.CreateAsync();

        var tools = await fixture.Client.ListToolsAsync(cancellationToken: CancellationToken.None);

        Assert.Contains(tools, tool => tool.Name == "echo");
        Assert.Contains(tools, tool => tool.Name == "utc_now");
    }

    [Fact]
    public async Task CallEchoTool_ReturnsExpectedStructuredContent()
    {
        await using var fixture = await SdkServerFixture.CreateAsync();

        var result = await fixture.Client.CallToolAsync(
            "echo",
            new Dictionary<string, object?> { ["text"] = "hello" },
            cancellationToken: CancellationToken.None);

        Assert.False(result.IsError ?? false);

        var textBlock = Assert.IsType<TextContentBlock>(Assert.Single(result.Content));
        Assert.Contains("hello", textBlock.Text, StringComparison.Ordinal);
    }

    private sealed class SdkServerFixture : IAsyncDisposable
    {
        private readonly CancellationTokenSource _shutdownTokenSource;
        private readonly IServiceProvider _serviceProvider;
        private readonly Task _serverTask;

        private SdkServerFixture(
            IServiceProvider serviceProvider,
            McpClient client,
            Task serverTask,
            CancellationTokenSource shutdownTokenSource)
        {
            _serviceProvider = serviceProvider;
            Client = client;
            _serverTask = serverTask;
            _shutdownTokenSource = shutdownTokenSource;
        }

        public McpClient Client { get; }

        public static async Task<SdkServerFixture> CreateAsync()
        {
            var clientToServerPipe = new Pipe();
            var serverToClientPipe = new Pipe();

            var services = new ServiceCollection();
            services.AddLogging();
            services
                .AddMcpServer()
                .WithStreamServerTransport(
                    clientToServerPipe.Reader.AsStream(),
                    serverToClientPipe.Writer.AsStream())
                .WithTools<EchoTool>()
                .WithTools<UtcNowTool>();

            var serviceProvider = services.BuildServiceProvider();
            var server = serviceProvider.GetRequiredService<McpServer>();
            var shutdownTokenSource = new CancellationTokenSource();
            var serverTask = server.RunAsync(shutdownTokenSource.Token);

            var client = await McpClient.CreateAsync(
                new StreamClientTransport(
                    clientToServerPipe.Writer.AsStream(),
                    serverToClientPipe.Reader.AsStream()),
                cancellationToken: CancellationToken.None);

            return new SdkServerFixture(serviceProvider, client, serverTask, shutdownTokenSource);
        }

        public async ValueTask DisposeAsync()
        {
            _shutdownTokenSource.Cancel();

            await Client.DisposeAsync();

            try
            {
                await _serverTask;
            }
            catch (OperationCanceledException)
            {
            }

            if (_serviceProvider is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _shutdownTokenSource.Dispose();
        }
    }
}
