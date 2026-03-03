using McpPlatform.Core.Tools;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTools.BuiltIn.Tools;

[McpServerToolType]
public sealed class EchoTool : IMcpTool
{
    [McpServerTool(Name = "echo", UseStructuredContent = true), Description("Returns the same text that was provided in the input.")]
    public static EchoResult Echo([Description("Text to echo back.")] string text)
    {
        return new EchoResult(text);
    }

    public sealed record EchoResult(string Echo);
}
