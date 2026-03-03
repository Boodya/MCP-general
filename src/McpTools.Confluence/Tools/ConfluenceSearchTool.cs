using McpPlatform.Core.Tools;
using McpTools.Confluence.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTools.Confluence.Tools;

[McpServerToolType]
public sealed class ConfluenceSearchTool(ConfluenceClient confluenceClient) : IMcpTool
{
    // ─── Search ───────────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_search", UseStructuredContent = true)]
    [Description(
        "Search Confluence pages and blog posts using CQL (Confluence Query Language). " +
        "Examples: 'text ~ \"deployment\"', " +
        "'space = \"DEV\" AND title ~ \"API\"', " +
        "'type = blogpost AND created > \"2025-01-01\"'. " +
        "Returns page IDs, titles, spaces, versions, and direct links.")]
    public async Task<ConfluenceSearchResponse> SearchAsync(
        [Description(
            "CQL query string. Use 'text ~ \"keyword\"' for full-text search, " +
            "'title ~ \"...\"' to filter by title, 'space = \"KEY\"' to limit to a space. " +
            "Combine with AND / OR. Quote values with spaces.")]
        string cql,

        [Description("Maximum number of results to return. Defaults to 25, max 50.")]
        int limit = 25,

        [Description("Zero-based offset for pagination.")]
        int start = 0,

        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var result = await confluenceClient.SearchAsync(cql, limit, start, cancellationToken);

        var baseUrl = result.Links?.Base ?? string.Empty;
        var items   = result.Results.Select(p => new ConfluencePageSummary(
            p.Id,
            p.Title,
            p.Space?.Key   ?? string.Empty,
            p.Space?.Name  ?? string.Empty,
            p.Version?.Number ?? 0,
            baseUrl + (p.Links?.WebUi ?? string.Empty))).ToList();

        return new ConfluenceSearchResponse(items, result.TotalSize, result.Start, result.Limit);
    }

    // ─── List spaces ──────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_list_spaces", UseStructuredContent = true)]
    [Description(
        "Lists accessible Confluence spaces (returns key, name, URL and pagination info). " +
        "Use start + limit to paginate through all spaces.")]
    public async Task<ConfluenceSpacesResponse> ListSpacesAsync(
        [Description("Maximum number of spaces to return (1–50). Defaults to 50.")]
        int limit = 50,

        [Description("Zero-based offset for pagination. Defaults to 0.")]
        int start = 0,

        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);
        var result = await confluenceClient.GetSpacesAsync(limit, start, cancellationToken);

        var items = result.Results.Select(s => new ConfluenceSpaceSummary(
            s.Key,
            s.Name,
            s.Type,
            s.Links?.WebUi ?? string.Empty)).ToList();

        return new ConfluenceSpacesResponse(items, result.Start, result.Limit, result.Size);
    }

    // ─── Response records ─────────────────────────────────────────────────────

    public sealed record ConfluenceSearchResponse(
        IReadOnlyList<ConfluencePageSummary> Results,
        int TotalSize,
        int Start,
        int Limit);

    public sealed record ConfluencePageSummary(
        string Id,
        string Title,
        string SpaceKey,
        string SpaceName,
        int    CurrentVersion,
        string Url);

    public sealed record ConfluenceSpacesResponse(
        IReadOnlyList<ConfluenceSpaceSummary> Spaces,
        int Start,
        int Limit,
        int ReturnedCount);

    public sealed record ConfluenceSpaceSummary(
        string Key,
        string Name,
        string Type,
        string Url);
}
