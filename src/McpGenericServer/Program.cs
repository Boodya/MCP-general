using McpPlatform.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

// Stdio MCP server — launched directly by the MCP client (e.g. VS Code).
// Tool implementations live in separate plugin assemblies under /plugins and are
// discovered automatically by PluginLoader at startup.

await McpStdioHostBuilder.RunAsync(args, (services, _) =>
{
    services
        .AddMcpServer()
        .WithStdioServerTransport();
});
