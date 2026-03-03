using System.Text.Json.Serialization;

namespace McpTools.Confluence.Services;

// ─── Common primitives ────────────────────────────────────────────────────────

public sealed record ConfluenceSpace(
    [property: JsonPropertyName("key")]   string Key,
    [property: JsonPropertyName("name")]  string Name);

public sealed record ConfluenceVersion(
    [property: JsonPropertyName("number")] int Number,
    [property: JsonPropertyName("message")] string? Message);

public sealed record ConfluenceBody(
    [property: JsonPropertyName("storage")] ConfluenceBodyValue? Storage,
    [property: JsonPropertyName("view")]    ConfluenceBodyValue? View);

public sealed record ConfluenceBodyValue(
    [property: JsonPropertyName("value")]          string Value,
    [property: JsonPropertyName("representation")] string Representation);

public sealed record ConfluenceLinks(
    [property: JsonPropertyName("webui")]  string? WebUi,
    [property: JsonPropertyName("base")]   string? Base,
    [property: JsonPropertyName("self")]   string? Self);

// ─── Page / Content ───────────────────────────────────────────────────────────

public sealed record ConfluencePage(
    [property: JsonPropertyName("id")]      string Id,
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("title")]   string Title,
    [property: JsonPropertyName("space")]   ConfluenceSpace? Space,
    [property: JsonPropertyName("version")] ConfluenceVersion? Version,
    [property: JsonPropertyName("body")]    ConfluenceBody? Body,
    [property: JsonPropertyName("_links")]  ConfluenceLinks? Links);

// ─── Search results ───────────────────────────────────────────────────────────

public sealed record ConfluenceSearchResult(
    [property: JsonPropertyName("results")]    IReadOnlyList<ConfluencePage> Results,
    [property: JsonPropertyName("start")]      int Start,
    [property: JsonPropertyName("limit")]      int Limit,
    [property: JsonPropertyName("size")]       int Size,
    [property: JsonPropertyName("totalSize")]  int TotalSize,
    [property: JsonPropertyName("_links")]     ConfluenceLinks? Links);

// ─── Spaces ───────────────────────────────────────────────────────────────────

public sealed record ConfluenceSpaceItem(
    [property: JsonPropertyName("id")]   long   Id,
    [property: JsonPropertyName("key")]  string Key,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("_links")] ConfluenceLinks? Links);

public sealed record ConfluenceSpaceResult(
    [property: JsonPropertyName("results")]   IReadOnlyList<ConfluenceSpaceItem> Results,
    [property: JsonPropertyName("size")]      int Size,
    [property: JsonPropertyName("start")]     int Start,
    [property: JsonPropertyName("limit")]     int Limit);

// ─── Create / Update request bodies ──────────────────────────────────────────

public sealed record CreatePageRequest(
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("title")]   string Title,
    [property: JsonPropertyName("space")]   SpaceRef Space,
    [property: JsonPropertyName("ancestors")] IReadOnlyList<IdRef>? Ancestors,
    [property: JsonPropertyName("body")]    BodyRequest Body);

public sealed record UpdatePageRequest(
    [property: JsonPropertyName("id")]      string Id,
    [property: JsonPropertyName("type")]    string Type,
    [property: JsonPropertyName("title")]   string Title,
    [property: JsonPropertyName("version")] VersionRef Version,
    [property: JsonPropertyName("body")]    BodyRequest Body);

public sealed record SpaceRef([property: JsonPropertyName("key")] string Key);
public sealed record IdRef([property: JsonPropertyName("id")]     string Id);
public sealed record VersionRef([property: JsonPropertyName("number")] int Number);
public sealed record BodyRequest([property: JsonPropertyName("storage")] StorageBody Storage);
public sealed record StorageBody(
    [property: JsonPropertyName("value")]          string Value,
    [property: JsonPropertyName("representation")] string Representation = "storage");
