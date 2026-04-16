using McpPlatform.Core.Tools;
using McpTools.Confluence.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpTools.Confluence.Tools;

[McpServerToolType]
public sealed class ConfluencePageTool(ConfluenceClient confluenceClient) : IMcpTool
{
    // ─── Get page ─────────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_get_page", UseStructuredContent = true)]
    [Description(
        "Retrieves the full content of a Confluence page by its numeric page ID. " +
        "Returns the rendered HTML view (body.view) for reading AND the storage format " +
        "(body.storage, Confluence XHTML) for editing purposes, plus version and space info. " +
        "Use confluence_search first to find the page ID.")]
    public async Task<ConfluencePageResponse> GetPageAsync(
        [Description("The numeric Confluence page ID (e.g. '123456').")]
        string pageId,

        CancellationToken cancellationToken = default)
    {
        var page  = await confluenceClient.GetPageAsync(pageId, cancellationToken);
        return MapToResponse(page);
    }

    // ─── Create page ──────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_create_page", UseStructuredContent = true)]
    [Description(
        "Creates a new Confluence page in the specified space. " +
        "Returns the newly created page with its ID and URL. " +
        "Content can be in Confluence Storage Format (XHTML) or Wiki Markup — " +
        "set the 'representation' parameter accordingly. " +
        "Storage format example: '<p>Hello world</p>'. " +
        "Wiki markup example: 'h2. Hello\n*bold* text'. " +
        "Optionally provide a parent page ID to nest the page in the hierarchy.")]
    public async Task<ConfluencePageResponse> CreatePageAsync(
        [Description("Space key where the page will be created (e.g. 'DEV', 'TEAM').")]
        string spaceKey,

        [Description("Title of the new page.")]
        string title,

        [Description(
            "Page body content. Format depends on the 'representation' parameter. " +
            "Storage format example: '<p>This is <strong>bold</strong> text.</p>'. " +
            "Wiki markup example: 'h2. Heading\n*bold* _italic_'.")]
        string storageContent,

        [Description(
            "Content format: 'storage' for Confluence Storage Format (XHTML), " +
            "'wiki' for Confluence Wiki Markup. Defaults to 'storage'.")]
        string representation = "storage",

        [Description("Optional parent page ID. When provided, the new page is created as a child.")]
        string? parentPageId = null,

        CancellationToken cancellationToken = default)
    {
        var ancestors = parentPageId is not null
            ? [new IdRef(parentPageId)]
            : (IReadOnlyList<IdRef>?)null;

        var request = new CreatePageRequest(
            Type:      "page",
            Title:     title,
            Space:     new SpaceRef(spaceKey),
            Ancestors: ancestors,
            Body:      new BodyRequest(new StorageBody(storageContent, representation)));

        var page = await confluenceClient.CreatePageAsync(request, cancellationToken);
        return MapToResponse(page);
    }

    // ─── Update page ──────────────────────────────────────────────────────────

    [McpServerTool(Name = "confluence_update_page", UseStructuredContent = true)]
    [Description(
        "Updates the title and/or body of an existing Confluence page. " +
        "You MUST provide the current version number (obtain it via confluence_get_page). " +
        "The API will automatically use version + 1 as the new version. " +
        "Content can be in Confluence Storage Format (XHTML) or Wiki Markup — " +
        "set the 'representation' parameter accordingly. " +
        "To preserve the existing body, first fetch it with confluence_get_page " +
        "and pass the storageContent back unchanged (with representation='storage').")]
    public async Task<ConfluencePageResponse> UpdatePageAsync(
        [Description("The numeric Confluence page ID to update.")]
        string pageId,

        [Description("New page title (can be same as current to keep it unchanged).")]
        string title,

        [Description(
            "New page body content. Format depends on the 'representation' parameter. " +
            "Replaces the entire page content.")]
        string storageContent,

        [Description(
            "The CURRENT version number of the page (as returned by confluence_get_page). " +
            "The update will be saved as version + 1.")]
        int currentVersion,

        [Description(
            "Content format: 'storage' for Confluence Storage Format (XHTML), " +
            "'wiki' for Confluence Wiki Markup. Defaults to 'storage'.")]
        string representation = "storage",

        CancellationToken cancellationToken = default)
    {
        var request = new UpdatePageRequest(
            Id:      pageId,
            Type:    "page",
            Title:   title,
            Version: new VersionRef(currentVersion + 1),
            Body:    new BodyRequest(new StorageBody(storageContent, representation)));

        var page = await confluenceClient.UpdatePageAsync(pageId, request, cancellationToken);
        return MapToResponse(page);
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    private static ConfluencePageResponse MapToResponse(ConfluencePage page)
    {
        var baseUrl = page.Links?.Base ?? string.Empty;
        return new ConfluencePageResponse(
            Id:             page.Id,
            Title:          page.Title,
            SpaceKey:       page.Space?.Key  ?? string.Empty,
            SpaceName:      page.Space?.Name ?? string.Empty,
            Version:        page.Version?.Number ?? 0,
            Url:            baseUrl + (page.Links?.WebUi ?? string.Empty),
            BodyView:       page.Body?.View?.Value    ?? string.Empty,
            BodyStorage:    page.Body?.Storage?.Value ?? string.Empty);
    }

    // ─── Response records ─────────────────────────────────────────────────────

    public sealed record ConfluencePageResponse(
        string Id,
        string Title,
        string SpaceKey,
        string SpaceName,
        int    Version,
        string Url,
        /// <summary>Rendered HTML — good for reading/summarising.</summary>
        string BodyView,
        /// <summary>Storage format (XHTML) — needed when updating the page.</summary>
        string BodyStorage);
}
