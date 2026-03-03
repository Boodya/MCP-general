using System.Reflection;
using System.Runtime.Loader;

namespace McpPlatform.Hosting.Loading;

/// <summary>
/// Isolated <see cref="AssemblyLoadContext"/> for a single plugin assembly.
/// Uses <see cref="AssemblyDependencyResolver"/> to resolve the plugin's own
/// dependencies from its directory, while sharing runtime and platform assemblies
/// with the host via the default context.
/// </summary>
internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;
    private readonly string _pluginDirectory;

    public PluginLoadContext(string pluginAssemblyPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginAssemblyPath), isCollectible: false)
    {
        _resolver        = new AssemblyDependencyResolver(pluginAssemblyPath);
        _pluginDirectory = Path.GetDirectoryName(pluginAssemblyPath)!;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from the plugin's own dependency graph first.
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
            return LoadFromAssemblyPath(assemblyPath);

        // Probe the plugin's directory directly for private dependencies that
        // were not listed in a .deps.json (e.g. NuGet packages copied alongside
        // the plugin DLL but not present in the host's output directory).
        var candidate = Path.Combine(_pluginDirectory, assemblyName.Name + ".dll");
        if (File.Exists(candidate))
            return LoadFromAssemblyPath(candidate);

        // Fall back to the default (host) context — this covers shared platform
        // assemblies like Microsoft.Extensions.*, ModelContextProtocol, etc.
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        return libraryPath is not null
            ? LoadUnmanagedDllFromPath(libraryPath)
            : IntPtr.Zero;
    }
}
