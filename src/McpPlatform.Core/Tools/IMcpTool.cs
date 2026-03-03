namespace McpPlatform.Core.Tools;

/// <summary>
/// Marker interface for MCP tool implementations.
/// Classes that implement this interface are auto-discovered by <see cref="IMcpPlugin"/>
/// implementations and registered as MCP server tools via the SDK.
/// </summary>
/// <remarks>
/// A class marked with <see cref="IMcpTool"/> should also carry the
/// <c>[McpServerToolType]</c> attribute (from ModelContextProtocol.Server) so the SDK
/// can introspect its tool methods.  The <see cref="IMcpTool"/> contract purely drives
/// reflection-based discovery at plugin load time.
/// </remarks>
public interface IMcpTool { }
