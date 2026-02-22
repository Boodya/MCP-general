# Connecting to McpGenericServer

This document explains how to **build**, **launch**, and **connect** to `McpGenericServer`
from any MCP-capable client — VS Code Copilot, Claude Desktop, the .NET/Python/TypeScript
MCP SDKs, or any other environment that speaks the
[Model Context Protocol](https://modelcontextprotocol.io).

---

## Prerequisites

| Requirement | Version |
|---|---|
| .NET SDK | 10.0 or later |
| OS | Windows, macOS, or Linux |

---

## 1. Build

```bash
# from the repository root
dotnet build McpGenericServer.sln
```

Or publish a self-contained binary for deployment:

```bash
dotnet publish src/McpGenericServer/McpGenericServer.csproj \
  -c Release \
  -o ./publish/McpGenericServer
```

---

## 2. Run the server manually

The server speaks **stdio MCP** — it reads JSON-RPC from `stdin` and writes to `stdout`.
You never launch it directly in a terminal for regular use; a client process spawns it.

To verify it starts without errors:

```bash
dotnet run --project src/McpGenericServer/McpGenericServer.csproj
# press Ctrl+C to stop
```

Or with a published binary:

```bash
./publish/McpGenericServer/McpGenericServer
```

---

## 3. VS Code (GitHub Copilot / Copilot Chat)

The repository already ships a ready-to-use configuration at
[`.vscode/mcp.json`](../.vscode/mcp.json).

```jsonc
// .vscode/mcp.json
{
  "servers": {
    "mcpGenericLocal": {
      "type": "stdio",
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "${workspaceFolder}/src/McpGenericServer/McpGenericServer.csproj"
      ],
      "env": {
        "DOTNET_NOLOGO": "1",
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1"
      }
    }
  }
}
```

VS Code reads this file automatically when the workspace is opened.
Copilot Chat will list the server's tools (e.g. `echo`, `utc_now`) and any tools
registered by plugins you have dropped into the `plugins/` folder.

> **Tip — use a published binary in production.**  
> Replace `"command": "dotnet"` / `"args": ["run", ...]` with the path to the
> published executable so VS Code doesn't re-build on every invocation:
>
> ```jsonc
> "command": "${workspaceFolder}/publish/McpGenericServer/McpGenericServer",
> "args": []
> ```

---

## 4. Claude Desktop

Add an entry to `claude_desktop_config.json`
(`%APPDATA%\Claude\claude_desktop_config.json` on Windows,
`~/Library/Application Support/Claude/claude_desktop_config.json` on macOS):

```json
{
  "mcpServers": {
    "mcpGenericLocal": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/Repos/MCP-general/src/McpGenericServer/McpGenericServer.csproj"
      ],
      "env": {
        "DOTNET_NOLOGO": "1",
        "DOTNET_CLI_TELEMETRY_OPTOUT": "1"
      }
    }
  }
}
```

Use the published binary path for a faster startup (same swap as in the VS Code tip above).

Restart Claude Desktop after editing, then check **Settings → Developer** to confirm
the server appears as connected.

---

## 5. Programmatic connection — .NET

Use the [ModelContextProtocol NuGet package](https://www.nuget.org/packages/ModelContextProtocol):

```csharp
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

var transport = new StdioClientTransport(new StdioClientTransportOptions
{
    Command = "dotnet",
    Arguments = [
        "run",
        "--project",
        "/path/to/src/McpGenericServer/McpGenericServer.csproj"
    ],
    EnvironmentVariables = new Dictionary<string, string>
    {
        ["DOTNET_NOLOGO"] = "1"
    }
});

await using var client = await McpClientFactory.CreateAsync(transport);

// List available tools
var tools = await client.ListToolsAsync();
foreach (var tool in tools)
    Console.WriteLine($"{tool.Name}: {tool.Description}");

// Call a tool
var result = await client.CallToolAsync("echo", new { text = "hello" });
Console.WriteLine(result.Content[0]);
```

---

## 6. Programmatic connection — Python

Using the [MCP Python SDK](https://github.com/modelcontextprotocol/python-sdk):

```python
import asyncio
from mcp import ClientSession, StdioServerParameters
from mcp.client.stdio import stdio_client

server_params = StdioServerParameters(
    command="dotnet",
    args=[
        "run",
        "--project",
        "/path/to/src/McpGenericServer/McpGenericServer.csproj"
    ],
    env={"DOTNET_NOLOGO": "1"},
)

async def main():
    async with stdio_client(server_params) as (read, write):
        async with ClientSession(read, write) as session:
            await session.initialize()

            tools = await session.list_tools()
            for tool in tools.tools:
                print(f"{tool.name}: {tool.description}")

            result = await session.call_tool("echo", {"text": "hello"})
            print(result.content[0].text)

asyncio.run(main())
```

---

## 7. Programmatic connection — TypeScript / Node.js

Using the [MCP TypeScript SDK](https://github.com/modelcontextprotocol/typescript-sdk):

```typescript
import { Client } from "@modelcontextprotocol/sdk/client/index.js";
import { StdioClientTransport } from "@modelcontextprotocol/sdk/client/stdio.js";

const transport = new StdioClientTransport({
  command: "dotnet",
  args: [
    "run",
    "--project",
    "/path/to/src/McpGenericServer/McpGenericServer.csproj",
  ],
  env: { DOTNET_NOLOGO: "1" },
});

const client = new Client({ name: "my-agent", version: "1.0.0" });
await client.connect(transport);

const { tools } = await client.listTools();
tools.forEach(t => console.log(`${t.name}: ${t.description}`));

const result = await client.callTool({ name: "echo", arguments: { text: "hello" } });
console.log(result.content[0]);

await client.close();
```

---

## 8. Environment / configuration

The server reads settings from `appsettings.json` (placed next to the binary) or
environment variables:

| Setting | Env variable | Default | Description |
|---|---|---|---|
| `Mcp:PluginsDirectory` | `Mcp__PluginsDirectory` | `plugins` | Path scanned for plugin `.dll` files |
| `Mcp:FailFastOnPluginError` | `Mcp__FailFastOnPluginError` | `false` | Abort startup on a bad plugin instead of skipping it |

Example `appsettings.json`:

```json
{
  "Mcp": {
    "PluginsDirectory": "plugins",
    "FailFastOnPluginError": false
  }
}
```

Pass them as environment variables from any MCP client config:

```json
"env": {
  "Mcp__PluginsDirectory": "/opt/mcp/plugins",
  "Mcp__FailFastOnPluginError": "true"
}
```

---

## 9. Loading plugins at runtime

Drop the plugin's build output into the configured `plugins/` directory:

```
McpGenericServer.exe          (or dotnet run)
└── plugins/
    ├── MyCompany.Mcp.Crm.dll
    ├── MyCompany.Mcp.Crm.deps.json   ← required for dependency resolution
    └── MyCompany.Mcp.Hr.dll
```

The server discovers them on the **next startup** — no recompilation of the server needed.
See the [main README](../README.md) for how to write a plugin.
