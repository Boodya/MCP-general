namespace McpTools.Confluence.Services;

/// <summary>
/// Configuration options bound from the <c>Confluence</c> section
/// (environment variables or appsettings.json).
/// </summary>
public sealed class ConfluenceOptions
{
    public const string SectionName = "Confluence";

    /// <summary>
    /// Base URL of the Confluence instance, e.g. <c>https://wiki/</c>.
    /// Trailing slash is optional — the client normalises it.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Authentication method: <c>Pat</c> (default) or <c>Basic</c>.
    /// </summary>
    public string AuthType { get; set; } = "Pat";

    /// <summary>
    /// Personal Access Token. Used when <see cref="AuthType"/> is <c>Pat</c>.
    /// Generate one under Confluence → Profile → Personal Access Tokens.
    /// </summary>
    public string PersonalAccessToken { get; set; } = string.Empty;

    /// <summary>Username for Basic auth.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// When <c>true</c>, SSL certificate validation is bypassed.
    /// Useful for self-signed or corporate-CA certificates not trusted by the .NET runtime.
    /// Defaults to <c>false</c>.
    /// </summary>
    public bool IgnoreSslErrors { get; set; } = false;

    /// <summary>Password for Basic auth.</summary>
    public string Password { get; set; } = string.Empty;
}
