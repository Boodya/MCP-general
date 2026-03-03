using McpPlatform.Core.Tools;
using McpTools.BuiltIn;
using McpTools.BuiltIn.Tools;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using System.Reflection;

namespace McpPlatform.Hosting.Tests;

public sealed class BuiltInToolsPluginTests
{
    // ── Metadata ────────────────────────────────────────────────────────────

    [Fact]
    public void Plugin_Name_IsSet()
    {
        var plugin = new BuiltInToolsPlugin();
        Assert.Equal("McpTools.BuiltIn", plugin.Name);
    }

    [Fact]
    public void Plugin_Version_IsSet()
    {
        var plugin = new BuiltInToolsPlugin();
        Assert.Equal("1.0.0", plugin.Version);
    }

    // ── IMcpTool contract ────────────────────────────────────────────────────

    [Fact]
    public void EchoTool_ImplementsIMcpTool()
    {
        Assert.True(typeof(IMcpTool).IsAssignableFrom(typeof(EchoTool)),
            $"{nameof(EchoTool)} must implement {nameof(IMcpTool)}.");
    }

    [Fact]
    public void UtcNowTool_ImplementsIMcpTool()
    {
        Assert.True(typeof(IMcpTool).IsAssignableFrom(typeof(UtcNowTool)),
            $"{nameof(UtcNowTool)} must implement {nameof(IMcpTool)}.");
    }

    // ── SDK attribute contract ───────────────────────────────────────────────

    [Fact]
    public void EchoTool_HasMcpServerToolTypeAttribute()
    {
        Assert.NotNull(typeof(EchoTool).GetCustomAttribute<McpServerToolTypeAttribute>());
    }

    [Fact]
    public void UtcNowTool_HasMcpServerToolTypeAttribute()
    {
        Assert.NotNull(typeof(UtcNowTool).GetCustomAttribute<McpServerToolTypeAttribute>());
    }

    // ── Auto-discovery ───────────────────────────────────────────────────────

    [Fact]
    public void BuiltInAssembly_ExposesAtLeastTwoIMcpToolTypes()
    {
        var toolTypes = typeof(BuiltInToolsPlugin).Assembly
            .GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IMcpTool).IsAssignableFrom(t))
            .ToList();

        Assert.True(toolTypes.Count >= 2,
            $"Expected at least 2 IMcpTool types in McpTools.BuiltIn, found {toolTypes.Count}.");
    }

    [Fact]
    public void BuiltInAssembly_HasExactlyOneIMcpPluginType()
    {
        var pluginTypes = typeof(BuiltInToolsPlugin).Assembly
            .GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(McpPlatform.Core.Plugins.IMcpPlugin).IsAssignableFrom(t))
            .ToList();

        Assert.Single(pluginTypes);
        Assert.Equal(typeof(BuiltInToolsPlugin), pluginTypes[0]);
    }

    // ── Registration ─────────────────────────────────────────────────────────

    [Fact]
    public void Register_DoesNotThrow()
    {
        // Arrange
        var plugin = new BuiltInToolsPlugin();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act / Assert — no exception means reflection-based tool discovery succeeded.
        var exception = Record.Exception(() => plugin.Register(services, configuration));
        Assert.Null(exception);
    }

    [Fact]
    public void Register_AddsServicesToContainer()
    {
        // Arrange
        var plugin = new BuiltInToolsPlugin();
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();
        int serviceCountBefore = services.Count;

        // Act
        plugin.Register(services, configuration);

        // Assert — the MCP SDK should have added services (tools, server, etc.)
        Assert.True(services.Count > serviceCountBefore,
            "Register() should add at least one service to the container.");
    }
}
