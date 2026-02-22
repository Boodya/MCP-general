namespace McpPlatform.Core.Plugins;

/// <summary>
/// Thrown when a plugin assembly cannot be loaded or fails during registration.
/// </summary>
public sealed class PluginLoadException : Exception
{
    /// <summary>Absolute path of the assembly that failed to load.</summary>
    public string AssemblyPath { get; }

    public PluginLoadException(string assemblyPath, string message, Exception? innerException = null)
        : base($"[{assemblyPath}] {message}", innerException)
    {
        AssemblyPath = assemblyPath;
    }
}
