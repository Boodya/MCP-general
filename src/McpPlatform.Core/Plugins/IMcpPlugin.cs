using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace McpPlatform.Core.Plugins;

/// <summary>
/// Entry point contract for every MCP plugin assembly.
/// Each plugin library must contain exactly one non-abstract implementation of this interface.
/// The host discovers it via reflection and calls <see cref="Register"/> during startup.
/// </summary>
public interface IMcpPlugin
{
    /// <summary>Human-readable plugin name used for logging and diagnostics.</summary>
    string Name { get; }

    /// <summary>SemVer string, e.g. "1.0.0".</summary>
    string Version { get; }

    /// <summary>
    /// Registers all plugin services, MCP tools, resources, and prompts
    /// into the host's DI container.
    /// </summary>
    /// <param name="services">The host service collection.</param>
    /// <param name="configuration">The host configuration (env vars, appsettings, etc.).</param>
    void Register(IServiceCollection services, IConfiguration configuration);
}
