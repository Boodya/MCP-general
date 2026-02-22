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

    public PluginLoadContext(string pluginAssemblyPath)
        : base(name: Path.GetFileNameWithoutExtension(pluginAssemblyPath), isCollectible: false)
    {
        _resolver = new AssemblyDependencyResolver(pluginAssemblyPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // Try to resolve from the plugin's own dependency graph first.
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath is not null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

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
