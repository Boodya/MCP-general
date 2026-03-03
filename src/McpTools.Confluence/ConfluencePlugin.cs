using McpPlatform.Core.Plugins;
using McpPlatform.Core.Tools;
using McpTools.Confluence.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpTools.Confluence;

/// <summary>
/// Plugin entry-point that registers all Confluence MCP tools.
/// </summary>
/// <remarks>
/// Required configuration (environment variables or appsettings.json, section "Confluence"):
/// <list type="bullet">
///   <item><c>Confluence__BaseUrl</c> – e.g. <c>https://wiki/</c></item>
///   <item><c>Confluence__AuthType</c> – <c>Pat</c> (default) or <c>Basic</c></item>
///   <item><c>Confluence__PersonalAccessToken</c> – for PAT auth</item>
///   <item><c>Confluence__Username</c> / <c>Confluence__Password</c> – for Basic auth</item>
/// </list>
/// </remarks>
public sealed class ConfluencePlugin : IMcpPlugin
{
    public string Name    => "McpTools.Confluence";
    public string Version => "1.0.0";

    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // Bind options from the "Confluence" config section.
        // Uses the Action<T> overload to avoid a dependency on
        // Microsoft.Extensions.Options.ConfigurationExtensions.
        var section = configuration.GetSection(ConfluenceOptions.SectionName);
        services.Configure<ConfluenceOptions>(opts =>
        {
            opts.BaseUrl              = section[nameof(ConfluenceOptions.BaseUrl)]              ?? string.Empty;
            opts.AuthType             = section[nameof(ConfluenceOptions.AuthType)]             ?? "Pat";
            opts.PersonalAccessToken  = section[nameof(ConfluenceOptions.PersonalAccessToken)]  ?? string.Empty;
            opts.Username             = section[nameof(ConfluenceOptions.Username)]             ?? string.Empty;
            opts.Password             = section[nameof(ConfluenceOptions.Password)]             ?? string.Empty;
            if (bool.TryParse(section[nameof(ConfluenceOptions.IgnoreSslErrors)], out var ignore))
                opts.IgnoreSslErrors = ignore;
        });

        // Register typed HttpClient → ConfluenceClient.
        // When IgnoreSslErrors is enabled (e.g. self-signed / corporate CA), bypass SSL validation.
        services.AddHttpClient<ConfluenceClient>()
                .ConfigurePrimaryHttpMessageHandler(sp =>
                {
                    var opts = sp.GetRequiredService<IOptions<ConfluenceOptions>>().Value;
                    return new HttpClientHandler
                    {
                        ServerCertificateCustomValidationCallback =
                            opts.IgnoreSslErrors
                                ? HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
                                : null
                    };
                });

        // Discover all IMcpTool implementations in this assembly and register as MCP tools
        var toolTypes = typeof(ConfluencePlugin).Assembly
            .GetExportedTypes()
            .Where(t => t is { IsAbstract: false, IsInterface: false }
                        && typeof(IMcpTool).IsAssignableFrom(t));

        services
            .AddMcpServer()
            .WithTools(toolTypes);
    }
}
