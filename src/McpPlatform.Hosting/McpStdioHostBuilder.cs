using McpPlatform.Hosting.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace McpPlatform.Hosting;

/// <summary>
/// Convenience builder for creating a stdio MCP server with optional plugin loading.
/// Handles logging configuration, legacy flag removal, and plugin discovery.
/// </summary>
public static class McpStdioHostBuilder
{
    /// <summary>
    /// Creates and runs a stdio MCP host.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    /// <param name="configureServices">
    /// Callback to register additional services or MCP tools (e.g. built-in tools
    /// via <c>.WithTools&lt;T&gt;()</c>).
    /// </param>
    /// <param name="loadPlugins">
    /// When <c>true</c> (default), discovers and loads plugins from the plugins directory.
    /// </param>
    public static async Task RunAsync(
        string[] args,
        Action<IServiceCollection, IConfiguration>? configureServices = null,
        bool loadPlugins = true)
    {
        var sanitizedArgs = RemoveLegacyFlags(args);

        var builder = Host.CreateApplicationBuilder(sanitizedArgs);

        // When launched via 'dotnet run' by an MCP client, the process working directory
        // is typically the client's workspace root, not the project/binary directory.
        // Add an explicit appsettings.json source from the binary output directory so that
        // the compiled configuration (including PluginIgnore etc.) is always found,
        // regardless of working directory.
        var binaryDirSettings = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        builder.Configuration.AddJsonFile(binaryDirSettings, optional: true, reloadOnChange: false);

        // MCP stdio servers must not write anything to stdout except protocol messages.
        // Route all log output to stderr.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options =>
        {
            options.LogToStandardErrorThreshold = LogLevel.Trace;
        });

        if (loadPlugins)
        {
            builder.Services.AddMcpPlugins(builder.Configuration);
        }

        configureServices?.Invoke(builder.Services, builder.Configuration);

        await builder.Build().RunAsync();
    }

    private static string[] RemoveLegacyFlags(string[] args) =>
        args
            .Where(arg => !string.Equals(arg, "--stdio", StringComparison.OrdinalIgnoreCase))
            .Where(arg => !string.Equals(arg, "--ui", StringComparison.OrdinalIgnoreCase))
            .ToArray();
}
