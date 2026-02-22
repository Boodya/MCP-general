# MCP Platform (.NET)

Plugin-based MCP server platform. Write tools once, deploy anywhere.

## Package structure

| Package | Role |
|---|---|
| `McpPlatform.Core` | Contracts only — `IMcpPlugin`, `PluginDescriptor`, `PluginLoadException` |
| `McpPlatform.Hosting` | Plugin loader, `McpStdioHostBuilder`, DI extensions |
| `McpGenericServer` | Reference stdio server — runs built-in tools + loads plugins dynamically |

## How plugins work

At startup, `McpGenericServer` scans the `plugins/` directory.  
Every `.dll` that contains a class implementing `IMcpPlugin` is loaded into an isolated  
`AssemblyLoadContext` and its `Register()` method is called.  
The plugin registers its own tools, HTTP clients, config bindings — anything it needs.

```
McpGenericServer.exe
└── plugins/
    ├── MyCompany.Mcp.Crm.dll       ← loaded automatically
    ├── MyCompany.Mcp.Crm.deps.json ← required for dependency resolution
    └── MyCompany.Mcp.Hr.dll        ← loaded automatically
```

## Writing a plugin (in a separate repo)

### 1. Install NuGet

```xml
<PackageReference Include="McpPlatform.Core" Version="1.0.0" />
<PackageReference Include="ModelContextProtocol" Version="0.8.0-preview.1" />
```

### 2. Implement IMcpPlugin

```csharp
using McpPlatform.Core.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

public sealed class CrmPlugin : IMcpPlugin
{
    public string Name => "Crm";
    public string Version => "1.0.0";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<ICrmClient, CrmClient>(client =>
        {
            client.BaseAddress = new Uri(configuration["Crm:BaseUrl"]!);
        });

        services
            .AddMcpServer()
            .WithTools<CrmTools>();
    }
}
```

### 3. Write your tools normally

```csharp
[McpServerToolType]
public sealed class CrmTools
{
    private readonly ICrmClient _crm;

    public CrmTools(ICrmClient crm) => _crm = crm;

    [McpServerTool(Name = "crm_get_customer"), Description("Returns customer by ID.")]
    public async Task<CustomerResult> GetCustomer(
        [Description("Customer ID")] string id,
        CancellationToken cancellationToken)
    {
        var customer = await _crm.GetCustomerAsync(id, cancellationToken);
        return new CustomerResult(customer.Id, customer.Name);
    }

    public sealed record CustomerResult(string Id, string Name);
}
```

### 4. Deploy

Copy the plugin's build output into `plugins/` next to `McpGenericServer.exe`.  
The server discovers and loads it on next startup — no recompilation needed.

## Configuration

`appsettings.json` or environment variables:

```json
{
  "Mcp": {
    "PluginsDirectory": "plugins",
    "FailFastOnPluginError": false
  }
}
```

## Connecting clients

See **[docs/connecting-clients.md](docs/connecting-clients.md)** for step-by-step instructions on:

- VS Code / GitHub Copilot Chat (`.vscode/mcp.json`)
- Claude Desktop
- Programmatic connection from .NET, Python, and TypeScript
- Environment variables and `appsettings.json` configuration
- Placing plugins in the `plugins/` directory

## Build & test

```bash
dotnet build McpGenericServer.sln
dotnet test McpGenericServer.sln
```
