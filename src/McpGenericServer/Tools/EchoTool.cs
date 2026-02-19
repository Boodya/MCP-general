using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpGenericServer.Tools;

[McpServerToolType]
public sealed class EchoTool
{
    [McpServerTool(Name = "echo", UseStructuredContent = true), Description("Returns the same text that was provided in the input.")]
    public static EchoResult Echo([Description("Text to echo back.")] string text)
    {
        return new EchoResult(text);
    }

    public sealed record EchoResult(string Echo);
}
