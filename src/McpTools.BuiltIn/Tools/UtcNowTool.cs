using McpPlatform.Core.Tools;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTools.BuiltIn.Tools;

[McpServerToolType]
public sealed class UtcNowTool : IMcpTool
{
    [McpServerTool(Name = "utc_now", UseStructuredContent = true), Description("Returns current UTC time in ISO-8601 format.")]
    public static UtcNowResult GetUtcNow()
    {
        return new UtcNowResult(DateTimeOffset.UtcNow.ToString("O"));
    }

    public sealed record UtcNowResult(string UtcNow);
}
