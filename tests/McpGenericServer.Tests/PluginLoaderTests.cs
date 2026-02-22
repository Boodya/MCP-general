using McpPlatform.Core.Plugins;
using McpPlatform.Hosting.Loading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpPlatform.Hosting.Tests;

public sealed class PluginLoaderTests
{
    private static PluginLoader CreateLoader() =>
        new(NullLogger<PluginLoader>.Instance);

    private static IConfiguration EmptyConfiguration() =>
        new ConfigurationBuilder().Build();

    [Fact]
    public void LoadPlugins_WhenDirectoryDoesNotExist_ReturnsEmptyList()
    {
        // Arrange
        var loader = CreateLoader();
        var services = new ServiceCollection();

        // Act
        var result = loader.LoadPlugins(
            "/nonexistent/plugins",
            services,
            EmptyConfiguration());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void LoadPlugins_WhenDirectoryIsEmpty_ReturnsEmptyList()
    {
        // Arrange
        var loader = CreateLoader();
        var services = new ServiceCollection();
        var dir = CreateTempDirectory();

        // Act
        var result = loader.LoadPlugins(dir, services, EmptyConfiguration());

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void LoadPlugins_WhenAssemblyHasNoPlugin_SkipsIt()
    {
        // Arrange
        // Copy a known .NET assembly that has no IMcpPlugin (xunit itself) into a temp dir.
        var loader = CreateLoader();
        var services = new ServiceCollection();
        var dir = CreateTempDirectory();
        var xunitAssembly = typeof(FactAttribute).Assembly.Location;
        File.Copy(xunitAssembly, Path.Combine(dir, "xunit.core.dll"));

        // Act — should not throw, should return 0 plugins
        var result = loader.LoadPlugins(dir, services, EmptyConfiguration());

        // Assert
        Assert.Empty(result);
    }

    private static string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }
}
