using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace McpTools.Confluence.Services;

/// <summary>
/// Thin HTTP client wrapper for the Confluence REST API v1.
/// Supports Personal Access Token (PAT) and Basic (username:password) authentication,
/// both of which work on Confluence Data Center / Server.
/// </summary>
public sealed class ConfluenceClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy        = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition      = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly string     _baseUrl;

    public ConfluenceClient(HttpClient http, IOptions<ConfluenceOptions> options)
    {
        _http    = http;
        var opts = options.Value;

        // Normalise base URL – strip trailing slash
        _baseUrl = opts.BaseUrl.TrimEnd('/');

        // Apply authentication header once on the shared client
        if (opts.AuthType.Equals("Basic", StringComparison.OrdinalIgnoreCase))
        {
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{opts.Username}:{opts.Password}"));
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", credentials);
        }
        else // PAT (default)
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", opts.PersonalAccessToken);
        }

        _http.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
    }

    // ─── Search ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Searches for pages/blog posts using CQL.
    /// </summary>
    public async Task<ConfluenceSearchResult> SearchAsync(
        string cql,
        int    limit  = 25,
        int    start  = 0,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/rest/api/content/search"
                + $"?cql={Uri.EscapeDataString(cql)}"
                + $"&limit={limit}&start={start}"
                + "&expand=space,version";

        return await GetAsync<ConfluenceSearchResult>(url, ct);
    }

    // ─── Get page ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Retrieves a single page by its numeric ID, including body content.
    /// </summary>
    public async Task<ConfluencePage> GetPageAsync(
        string pageId,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/rest/api/content/{Uri.EscapeDataString(pageId)}"
                + "?expand=body.storage,body.view,version,space";

        return await GetAsync<ConfluencePage>(url, ct);
    }

    // ─── Spaces ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns the list of accessible spaces.
    /// </summary>
    public async Task<ConfluenceSpaceResult> GetSpacesAsync(
        int limit = 50,
        int start = 0,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/rest/api/space?limit={limit}&start={start}&type=global";
        return await GetAsync<ConfluenceSpaceResult>(url, ct);
    }

    // ─── Create page ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new page in the specified space.
    /// Content must be in Confluence Storage Format (XHTML).
    /// </summary>
    public async Task<ConfluencePage> CreatePageAsync(
        CreatePageRequest request,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/rest/api/content";
        return await PostAsync<CreatePageRequest, ConfluencePage>(url, request, ct);
    }

    // ─── Update page ──────────────────────────────────────────────────────────

    /// <summary>
    /// Updates an existing page.
    /// The <paramref name="request"/> must include the current version number + 1.
    /// Content must be in Confluence Storage Format (XHTML).
    /// </summary>
    public async Task<ConfluencePage> UpdatePageAsync(
        string            pageId,
        UpdatePageRequest request,
        CancellationToken ct = default)
    {
        var url = $"{_baseUrl}/rest/api/content/{Uri.EscapeDataString(pageId)}";
        return await PutAsync<UpdatePageRequest, ConfluencePage>(url, request, ct);
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        var response = await _http.GetAsync(url, ct);
        await EnsureSuccessAsync(response);
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return JsonSerializer.Deserialize<T>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Empty response body.");
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url, TRequest body, CancellationToken ct)
    {
        var content  = Serialize(body);
        var response = await _http.PostAsync(url, content, ct);
        await EnsureSuccessAsync(response);
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Empty response body.");
    }

    private async Task<TResponse> PutAsync<TRequest, TResponse>(
        string url, TRequest body, CancellationToken ct)
    {
        var content  = Serialize(body);
        var response = await _http.PutAsync(url, content, ct);
        await EnsureSuccessAsync(response);
        var stream = await response.Content.ReadAsStreamAsync(ct);
        return JsonSerializer.Deserialize<TResponse>(stream, JsonOptions)
            ?? throw new InvalidOperationException("Empty response body.");
    }

    private static StringContent Serialize<T>(T value) =>
        new(JsonSerializer.Serialize(value, JsonOptions),
            Encoding.UTF8, "application/json");

    private static async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException(
                $"Confluence API returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
        }
    }
}
