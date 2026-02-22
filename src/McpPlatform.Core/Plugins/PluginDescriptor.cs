namespace McpPlatform.Core.Plugins;

/// <summary>
/// Describes a successfully loaded plugin.
/// </summary>
/// <param name="Name">Human-readable plugin name.</param>
/// <param name="Version">SemVer plugin version string.</param>
/// <param name="AssemblyPath">Absolute path to the plugin assembly file.</param>
/// <param name="PluginType">The concrete <see cref="IMcpPlugin"/> type that was instantiated.</param>
public sealed record PluginDescriptor(
    string Name,
    string Version,
    string AssemblyPath,
    Type PluginType);
