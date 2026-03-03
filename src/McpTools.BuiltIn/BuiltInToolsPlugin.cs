using McpPlatform.Core.Plugins;
using McpPlatform.Core.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpTools.BuiltIn;

/// <summary>
/// Entry-point plugin for the built-in MCP tools (Echo, UtcNow, …).
/// The host's <c>PluginLoader</c> discovers this class, instantiates it, and calls
/// <see cref="Register"/> during startup to wire up all tools from this assembly.
/// </summary>
public sealed class BuiltInToolsPlugin : IMcpPlugin
{
    /// <inheritdoc/>
    public string Name => "McpTools.BuiltIn";

    /// <inheritdoc/>
    public string Version => "1.0.0";

    /// <inheritdoc/>
    /// <remarks>
    /// Scans the current assembly for every concrete <see cref="IMcpTool"/> implementation
    /// and registers each one with the MCP server using the non-generic
    /// <c>IMcpServerBuilder.WithTools(IEnumerable&lt;Type&gt;)</c> overload.
    /// Adding new tools to this project requires no changes here — just implement
    /// <see cref="IMcpTool"/> on the new class.
    /// </remarks>
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        var toolTypes = typeof(BuiltInToolsPlugin).Assembly
            .GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IMcpTool).IsAssignableFrom(t));

        services
            .AddMcpServer()
            .WithTools(toolTypes);
    }
}
