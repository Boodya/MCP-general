namespace McpPlatform.Hosting;

/// <summary>
/// Configuration options for the MCP host, bound from the <c>Mcp</c> config section.
/// </summary>
public sealed class McpHostOptions
{
    /// <summary>The configuration section name.</summary>
    public const string SectionName = "Mcp";

    /// <summary>
    /// Path to the directory that is scanned for plugin assemblies.
    /// Relative paths are resolved from the application base directory.
    /// Defaults to <c>plugins</c>.
    /// </summary>
    public string PluginsDirectory { get; set; } = "plugins";

    /// <summary>
    /// When <c>true</c>, a plugin load failure causes the host to refuse to start.
    /// When <c>false</c> (default), the failing plugin is skipped with a warning.
    /// </summary>
    public bool FailFastOnPluginError { get; set; } = false;

    /// <summary>
    /// File names of plugin assemblies to exclude from loading.
    /// Only the bare file name is matched (e.g. <c>"McpTools.Confluence.dll"</c>),
    /// not a full or relative path. Comparison is case-insensitive.
    /// </summary>
    /// <example>
    /// <code>
    /// "Mcp": {
    ///   "PluginIgnore": [ "McpTools.Confluence.dll" ]
    /// }
    /// </code>
    /// </example>
    public List<string> PluginIgnore { get; set; } = [];
}
