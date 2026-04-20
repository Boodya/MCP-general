using McpPlatform.Core.Tools;
using McpTools.Confluence.Services;
using ModelContextProtocol.Server;
using System.ComponentModel;
using System.Security;
using System.Text;

namespace McpTools.Confluence.Tools;

[McpServerToolType]
public sealed class ConfluenceDiagramTool(ConfluenceClient confluenceClient) : IMcpTool
{
    // ─── Create draw.io diagram ───────────────────────────────────────────────

    [McpServerTool(Name = "confluence_create_drawio_diagram", UseStructuredContent = true)]
    [Description(
        "Creates a draw.io diagram on an existing Confluence page. " +
        "The diagram is fully editable in Confluence via the draw.io editor. " +
        "Provide nodes (boxes) and edges (arrows) as JSON arrays. " +
        "After creation, the diagram appears on the page and users can click to edit it. " +
        "IMPORTANT: This REPLACES the page body with the diagram macro. " +
        "To add a diagram to an existing page, first get the page content via confluence_get_page, " +
        "then use confluence_update_page to insert the macro XML returned here alongside existing content.")]
    public async Task<CreateDiagramResponse> CreateDiagramAsync(
        [Description("The numeric Confluence page ID where the diagram will be created.")]
        string pageId,

        [Description(
            "JSON array of nodes. Each node: {\"id\":\"n1\",\"label\":\"Start\",\"x\":100,\"y\":100," +
            "\"width\":140,\"height\":60,\"shape\":\"RoundedRect\",\"fillColor\":\"#dae8fc\",\"strokeColor\":\"#6c8ebf\"}. " +
            "Shapes: Rectangle, RoundedRect, Ellipse, Diamond, Cylinder, Hexagon, Parallelogram, StartEnd. " +
            "Position (x,y) is in pixels from top-left. Colors are hex (#RRGGBB).")]
        string nodesJson,

        [Description(
            "JSON array of edges. Each edge: {\"id\":\"e1\",\"sourceId\":\"n1\",\"targetId\":\"n2\"," +
            "\"label\":\"next\",\"style\":\"Solid\"}. Styles: Solid, Dashed.")]
        string edgesJson,

        [Description("Optional diagram name (without extension). Defaults to 'Diagram-{timestamp}'.")]
        string? diagramName = null,

        CancellationToken cancellationToken = default)
    {
        // Parse nodes and edges
        var nodes = System.Text.Json.JsonSerializer.Deserialize<List<DiagramNode>>(nodesJson, JsonOptions)
            ?? throw new ArgumentException("Invalid nodes JSON.", nameof(nodesJson));
        var edges = System.Text.Json.JsonSerializer.Deserialize<List<DiagramEdge>>(edgesJson, JsonOptions)
            ?? throw new ArgumentException("Invalid edges JSON.", nameof(edgesJson));

        // Generate diagram name
        var name = diagramName ?? $"Diagram-{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        // Build mxGraph XML
        var mxGraphXml = MxGraphBuilder.Build(nodes, edges);
        var xmlBytes   = Encoding.UTF8.GetBytes(mxGraphXml);

        // Upload the draw.io XML as an attachment (the main diagram file)
        await confluenceClient.UploadAttachmentAsync(
            pageId, name, xmlBytes, "application/octet-stream",
            "draw.io diagram created via MCP", cancellationToken);

        // Generate and upload a minimal placeholder PNG so the macro can render something.
        // draw.io will regenerate the real PNG when a user first opens/saves the diagram.
        var placeholderPng = GeneratePlaceholderPng();
        await confluenceClient.UploadAttachmentAsync(
            pageId, $"{name}.png", placeholderPng, "image/png",
            "draw.io diagram preview (auto-generated)", cancellationToken);

        // Build the Confluence storage-format macro
        var macroId = Guid.NewGuid().ToString();

        // Calculate approximate diagram bounds for width/height
        var maxX = nodes.Max(n => n.X + n.Width);
        var maxY = nodes.Max(n => n.Y + n.Height);

        var macroXml =
            $"""
            <ac:structured-macro ac:name="drawio" ac:schema-version="1" ac:macro-id="{macroId}">
              <ac:parameter ac:name="border">true</ac:parameter>
              <ac:parameter ac:name="diagramName">{SecurityElement.Escape(name)}</ac:parameter>
              <ac:parameter ac:name="simpleViewer">false</ac:parameter>
              <ac:parameter ac:name="links">auto</ac:parameter>
              <ac:parameter ac:name="tbstyle">top</ac:parameter>
              <ac:parameter ac:name="lbox">true</ac:parameter>
              <ac:parameter ac:name="diagramWidth">{maxX + 40}</ac:parameter>
              <ac:parameter ac:name="height">{maxY + 40}</ac:parameter>
              <ac:parameter ac:name="revision">1</ac:parameter>
            </ac:structured-macro>
            """;

        return new CreateDiagramResponse(
            DiagramName: name,
            MacroXml:    macroXml,
            NodesCount:  nodes.Count,
            EdgesCount:  edges.Count,
            Hint:        "The diagram attachments have been uploaded. " +
                         "Use confluence_update_page to insert the MacroXml into the page body (storage format). " +
                         "Users can then click the diagram in Confluence to open the draw.io editor.");
    }

    // ─── Insert diagram into page (convenience method) ────────────────────────

    [McpServerTool(Name = "confluence_insert_drawio_diagram", UseStructuredContent = true)]
    [Description(
        "Creates a draw.io diagram AND inserts it into the Confluence page body in one step. " +
        "This replaces the entire page body with the diagram macro. " +
        "Use confluence_create_drawio_diagram instead if you want to manually compose the page content.")]
    public async Task<InsertDiagramResponse> InsertDiagramAsync(
        [Description("The numeric Confluence page ID.")]
        string pageId,

        [Description(
            "JSON array of nodes. Each node: {\"id\":\"n1\",\"label\":\"Start\",\"x\":100,\"y\":100," +
            "\"width\":140,\"height\":60,\"shape\":\"RoundedRect\",\"fillColor\":\"#dae8fc\",\"strokeColor\":\"#6c8ebf\"}.")]
        string nodesJson,

        [Description(
            "JSON array of edges. Each edge: {\"id\":\"e1\",\"sourceId\":\"n1\",\"targetId\":\"n2\"," +
            "\"label\":\"next\",\"style\":\"Solid\"}.")]
        string edgesJson,

        [Description("Optional diagram name (without extension). Defaults to 'Diagram-{timestamp}'.")]
        string? diagramName = null,

        CancellationToken cancellationToken = default)
    {
        // First, create the diagram attachments
        var createResult = await CreateDiagramAsync(
            pageId, nodesJson, edgesJson, diagramName, cancellationToken);

        // Get current page version
        var page = await confluenceClient.GetPageAsync(pageId, cancellationToken);
        var currentVersion = page.Version?.Number
            ?? throw new InvalidOperationException("Could not read page version.");

        // Update page body with the macro
        var updateRequest = new UpdatePageRequest(
            Id:      pageId,
            Type:    "page",
            Title:   page.Title,
            Version: new VersionRef(currentVersion + 1),
            Body:    new BodyRequest(new StorageBody($"<p>{createResult.MacroXml}</p>")));

        var updated = await confluenceClient.UpdatePageAsync(pageId, updateRequest, cancellationToken);

        var baseUrl = updated.Links?.Base ?? string.Empty;
        return new InsertDiagramResponse(
            PageId:      updated.Id,
            PageTitle:   updated.Title,
            PageUrl:     baseUrl + (updated.Links?.WebUi ?? string.Empty),
            DiagramName: createResult.DiagramName,
            Version:     updated.Version?.Number ?? 0,
            Message:     "Diagram created and inserted into the page. " +
                         "Click the diagram in Confluence to open the draw.io editor.");
    }

    // ─── Placeholder PNG ──────────────────────────────────────────────────────

    /// <summary>
    /// Returns a minimal valid 1x1 white PNG (67 bytes).
    /// draw.io will regenerate the real preview on first edit/save.
    /// </summary>
    private static byte[] GeneratePlaceholderPng()
    {
        // Minimal valid PNG: 1x1 pixel, white, RGBA
        return [
            0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, // PNG signature
            0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52, // IHDR chunk
            0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, // 1x1
            0x08, 0x02, 0x00, 0x00, 0x00, 0x90, 0x77, 0x53, // 8-bit RGB
            0xDE, 0x00, 0x00, 0x00, 0x0C, 0x49, 0x44, 0x41, // IDAT chunk
            0x54, 0x08, 0xD7, 0x63, 0xF8, 0xCF, 0xC0, 0x00, // compressed data
            0x00, 0x00, 0x02, 0x00, 0x01, 0xE2, 0x21, 0xBC, // checksum
            0x33, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45, 0x4E, // IEND chunk
            0x44, 0xAE, 0x42, 0x60, 0x82,
        ];
    }

    // ─── JSON options ─────────────────────────────────────────────────────────

    private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() },
    };

    // ─── Response records ─────────────────────────────────────────────────────

    public sealed record CreateDiagramResponse(
        string DiagramName,
        string MacroXml,
        int    NodesCount,
        int    EdgesCount,
        string Hint);

    public sealed record InsertDiagramResponse(
        string PageId,
        string PageTitle,
        string PageUrl,
        string DiagramName,
        int    Version,
        string Message);
}
