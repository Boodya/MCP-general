# MCP Generic Server (.NET)

MCP server based on the official `modelcontextprotocol/csharp-sdk` packages.

## What is included

- Official SDK hosting via `AddMcpServer()`
- Stdio transport via `.WithStdioServerTransport()`
- Tool registration via SDK attributes (`[McpServerToolType]`, `[McpServerTool]`)
- Two sample tools:
  - `echo`
  - `utc_now`
- Integration tests using official client/server transports

## Project layout

- `src/McpGenericServer/Tools` — MCP SDK tool types
- `src/McpGenericServer/Program.cs` — MCP host bootstrap for stdio
- `tests/McpGenericServer.Tests` — MCP SDK integration tests

## Run

```bash
dotnet run --project src/McpGenericServer/McpGenericServer.csproj
```

The process communicates over stdin/stdout using the official SDK transport and is intended to be launched by an MCP client/host.

## Test

```bash
dotnet test McpGenericServer.sln
```

## Add a new tool

1. Create a class in `src/McpGenericServer/Tools` and mark it with `[McpServerToolType]`.
2. Add a method with `[McpServerTool]` and `Description` attributes.
3. Register tool type in `Program.cs` with `.WithTools<YourToolType>()`.

Example pattern:

```csharp
[McpServerToolType]
public sealed class MyTools
{
  [McpServerTool(Name = "my_tool")]
  public static string MyTool(string input) => $"You sent: {input}";
}
```
