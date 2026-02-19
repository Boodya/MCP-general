using McpGenericServer.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

await RunStdioAsync(RemoveLegacyFlags(args));

return;

static async Task RunStdioAsync(string[] args)
{
    var builder = Host.CreateApplicationBuilder(args);
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(options =>
    {
        options.LogToStandardErrorThreshold = LogLevel.Trace;
    });

    RegisterStdioServer(builder.Services);

    await builder.Build().RunAsync();
}

static void RegisterStdioServer(IServiceCollection services)
{
    services
        .AddMcpServer()
        .WithStdioServerTransport()
        .WithTools<EchoTool>()
        .WithTools<UtcNowTool>();
}

static string[] RemoveLegacyFlags(string[] args)
{
    return args
        .Where(arg => !string.Equals(arg, "--stdio", StringComparison.OrdinalIgnoreCase))
        .Where(arg => !string.Equals(arg, "--ui", StringComparison.OrdinalIgnoreCase))
        .ToArray();
}
