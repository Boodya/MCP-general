using McpPlatform.Hosting.Loading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpPlatform.Hosting.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP platform plugin loader and discovers plugins
    /// from the configured <see cref="McpHostOptions.PluginsDirectory"/>.
    /// </summary>
    /// <remarks>
    /// Call this <em>before</em> <c>AddMcpServer()</c> so that plugin tools
    /// are registered into the DI container prior to MCP server initialization.
    /// </remarks>
    public static IServiceCollection AddMcpPlugins(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<McpHostOptions>? configureOptions = null)
    {
        services.Configure<McpHostOptions>(configuration.GetSection(McpHostOptions.SectionName));

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        // Use a temporary service provider to resolve logger and options for plugin loading.
        // Plugin DI registrations are added back into the original services collection.
        using var bootstrap = services
            .BuildServiceProvider();

        var options = bootstrap.GetRequiredService<IOptions<McpHostOptions>>().Value;
        var logger = bootstrap.GetRequiredService<ILoggerFactory>().CreateLogger<PluginLoader>();

        // Resolve relative paths against the application base directory (where the DLLs live),
        // not the current working directory (which may be the user's home folder when launched
        // by an MCP client via 'dotnet run').
        var pluginsDirectory = Path.IsPathRooted(options.PluginsDirectory)
            ? options.PluginsDirectory
            : Path.GetFullPath(options.PluginsDirectory, AppContext.BaseDirectory);

        var loader = new PluginLoader(logger);
        loader.LoadPlugins(pluginsDirectory, services, configuration);

        return services;
    }
}
