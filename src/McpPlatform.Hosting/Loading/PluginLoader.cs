using McpPlatform.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace McpPlatform.Hosting.Loading;

/// <summary>
/// Scans a plugins directory, loads each assembly in its own isolated
/// <see cref="PluginLoadContext"/>, finds the <see cref="IMcpPlugin"/> implementation,
/// and calls <see cref="IMcpPlugin.Register"/> to integrate it with the host DI container.
/// </summary>
public sealed class PluginLoader
{
    private readonly ILogger<PluginLoader> _logger;

    public PluginLoader(ILogger<PluginLoader> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Loads all plugins found in <paramref name="pluginsDirectory"/>.
    /// Each top-level <c>.dll</c> file that contains exactly one <see cref="IMcpPlugin"/>
    /// implementation is treated as a plugin entry point.
    /// </summary>
    /// <param name="pluginsDirectory">Absolute or relative path to the plugins folder.</param>
    /// <param name="services">Host DI container to register plugin services into.</param>
    /// <param name="configuration">Host configuration passed to each plugin.</param>
    /// <returns>Descriptors of successfully loaded plugins.</returns>
    public IReadOnlyList<PluginDescriptor> LoadPlugins(
        string pluginsDirectory,
        IServiceCollection services,
        IConfiguration configuration)
    {
        if (!Directory.Exists(pluginsDirectory))
        {
            _logger.LogWarning("Plugins directory '{Directory}' does not exist. No plugins loaded.", pluginsDirectory);
            return [];
        }

        var descriptors = new List<PluginDescriptor>();

        foreach (var assemblyPath in Directory.EnumerateFiles(pluginsDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                var descriptor = LoadSinglePlugin(assemblyPath, services, configuration);
                if (descriptor is not null)
                {
                    descriptors.Add(descriptor);
                    _logger.LogInformation(
                        "Plugin loaded: {Name} v{Version} from '{Path}'",
                        descriptor.Name, descriptor.Version, descriptor.AssemblyPath);
                }
            }
            catch (PluginLoadException ex)
            {
                // Non-fatal: log and continue loading remaining plugins.
                _logger.LogError(ex, "Plugin load failed, skipping assembly '{Path}'", assemblyPath);
            }
        }

        _logger.LogInformation("{Count} plugin(s) loaded from '{Directory}'", descriptors.Count, pluginsDirectory);
        return descriptors;
    }

    private PluginDescriptor? LoadSinglePlugin(
        string assemblyPath,
        IServiceCollection services,
        IConfiguration configuration)
    {
        Assembly assembly;
        try
        {
            var loadContext = new PluginLoadContext(assemblyPath);
            assembly = loadContext.LoadFromAssemblyPath(assemblyPath);
        }
        catch (Exception ex)
        {
            throw new PluginLoadException(assemblyPath, "Could not load assembly.", ex);
        }

        var pluginType = FindPluginType(assembly, assemblyPath);
        if (pluginType is null)
        {
            // Not every dll in the folder is necessarily a plugin — skip silently.
            _logger.LogDebug("Assembly '{Path}' has no IMcpPlugin implementation, skipping.", assemblyPath);
            return null;
        }

        IMcpPlugin plugin;
        try
        {
            plugin = (IMcpPlugin)Activator.CreateInstance(pluginType)!;
        }
        catch (Exception ex)
        {
            throw new PluginLoadException(assemblyPath, $"Could not instantiate '{pluginType.FullName}'.", ex);
        }

        try
        {
            plugin.Register(services, configuration);
        }
        catch (Exception ex)
        {
            throw new PluginLoadException(assemblyPath, $"Plugin '{plugin.Name}' threw during Register().", ex);
        }

        return new PluginDescriptor(plugin.Name, plugin.Version, assemblyPath, pluginType);
    }

    private static Type? FindPluginType(Assembly assembly, string assemblyPath)
    {
        Type[] pluginTypes;
        try
        {
            pluginTypes = assembly
                .GetExportedTypes()
                .Where(t => t is { IsAbstract: false, IsInterface: false }
                            && typeof(IMcpPlugin).IsAssignableFrom(t))
                .ToArray();
        }
        catch (Exception ex)
        {
            throw new PluginLoadException(assemblyPath, "Could not enumerate exported types.", ex);
        }

        return pluginTypes.Length switch
        {
            0 => null,
            1 => pluginTypes[0],
            _ => throw new PluginLoadException(
                assemblyPath,
                $"Assembly contains {pluginTypes.Length} IMcpPlugin implementations. " +
                $"Only one is allowed per assembly: {string.Join(", ", pluginTypes.Select(t => t.FullName))}")
        };
    }
}
