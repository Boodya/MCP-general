using McpGenericServer.Tools;
using McpPlatform.Hosting.Extensions;

// HTTP MCP server hosted in IIS via ASP.NET Core.
// Exposes:
//   GET  /sse      – SSE stream (MCP session initiation)
//   POST /message  – MCP message endpoint
//
// Plugin DLLs placed in /plugins are discovered and loaded automatically.

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMcpPlugins(builder.Configuration);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithTools<EchoTool>()
    .WithTools<UtcNowTool>();

var app = builder.Build();

app.MapMcp();

await app.RunAsync();
