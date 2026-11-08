using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

/// <summary>
/// Google sign-in for a desktop app: the system browser, a loopback redirect and PKCE (RFC 8252). The refresh token is kept
/// in a file encrypted for the current Windows user (DPAPI); the access token only in memory.
/// The OAuth client (a "Desktop app" client from the person's own Google Cloud project) comes from the settings or from
/// google-client.json next to the exe.
/// </summary>
public sealed class GoogleAuth
{
    public const string Scope = "openid email https://www.googleapis.com/auth/calendar.events";
    const string AuthUrl = "https://accounts.google.com/o/oauth2/v2/auth";
    const string TokenUrl = "https://oauth2.googleapis.com/token";
    const string RevokeUrl = "https://oauth2.googleapis.com/revoke";

    sealed class Stored
    {
        [JsonPropertyName("refreshToken")] public string RefreshToken { get; set; } = "";
        [JsonPropertyName("email")] public string? Email { get; set; }
    }

    sealed class TokenResponse
    {
        [JsonPropertyName("access_token")] public string? AccessToken { get; set; }
        [JsonPropertyName("expires_in")] public int ExpiresIn { get; set; }
        [JsonPropertyName("refresh_token")] public string? RefreshToken { get; set; }
        [JsonPropertyName("id_token")] public string? IdToken { get; set; }
        [JsonPropertyName("scope")] public string? Scope { get; set; }
        [JsonPropertyName("error")] public string? Error { get; set; }
        [JsonPropertyName("error_description")] public string? ErrorDescription { get; set; }
    }

    readonly HttpClient _http;
    readonly Func<(string Id, string Secret)> _client;
    readonly string _tokenFile;
    readonly SemaphoreSlim _gate = new(1, 1);
    Stored? _stored;
    string? _access;
    DateTime _accessExpiresUtc;

    public GoogleAuth(HttpClient http, Func<(string Id, string Secret)> client, string tokenFile)
    {
        _http = http;
        _client = client;
        _tokenFile = tokenFile;
        _stored = ReadFile();
    }

    public bool IsConnected => _stored is not null;
    public string? Email => _stored?.Email;
    public bool HasClient { get { var c = _client(); return c.Id.Length > 0; } }

    // ---------- connect ----------
    /// <summary>Opens the browser, waits for the person to allow access and stores the tokens. Returns the account's e-mail address.</summary>
    public async Task<string?> ConnectAsync(CancellationToken ct)
    {
        var (clientId, clientSecret) = _client();
        if (clientId.Length == 0) throw new GoogleAuthException("no OAuth client id is set");

        var verifier = Base64Url(RandomNumberGenerator.GetBytes(48));
        var challenge = Base64Url(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var state = Base64Url(RandomNumberGenerator.GetBytes(16));

        int port = FreePort();
        var redirect = $"http://127.0.0.1:{port}/";
        using var listener = new HttpListener();
        listener.Prefixes.Add(redirect);
        listener.Start();

        var url = AuthUrl
            + "?client_id=" + Uri.EscapeDataString(clientId)
            + "&redirect_uri=" + Uri.EscapeDataString(redirect)
            + "&response_type=code&scope=" + Uri.EscapeDataString(Scope)
            + "&code_challenge=" + challenge + "&code_challenge_method=S256"
            + "&state=" + state + "&access_type=offline&prompt=consent";
        Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromMinutes(3));
        using var stop = timeout.Token.Register(listener.Stop);

        string? code = null, error = null;
        try
        {
            while (code is null && error is null)
            {
                var ctx = await listener.GetContextAsync().ConfigureAwait(false);
                var q = ctx.Request.QueryString;
                if (q["state"] != state) { ctx.Response.StatusCode = 404; ctx.Response.Close(); continue; }   // favicon and other strays
                code = q["code"];
                error = q["error"] ?? (code is null ? "no code" : null);
                await Reply(ctx, error is null).ConfigureAwait(false);
            }
        }
        catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
        {
            ct.ThrowIfCancellationRequested();
            throw new GoogleAuthException("the sign-in was not completed in time");
        }
        if (error is not null) throw new GoogleAuthException("Google refused: " + error);

        var t = await PostAsync(TokenUrl, Form(clientId, clientSecret, new()
        {
            ["code"] = code!, ["redirect_uri"] = redirect, ["grant_type"] = "authorization_code", ["code_verifier"] = verifier
        }), ct).ConfigureAwait(false);

        if (t.AccessToken is null || string.IsNullOrEmpty(t.RefreshToken)) throw new GoogleAuthException("Google did not return a refresh token");
        if (t.Scope is null || !t.Scope.Contains("calendar.events", StringComparison.Ordinal))
            throw new GoogleAuthException("the access to the calendar was not granted");

        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            _stored = new Stored { RefreshToken = t.RefreshToken!, Email = EmailFrom(t.IdToken) };
            _access = t.AccessToken;
            _accessExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, t.ExpiresIn));
            WriteFile(_stored);
            return _stored.Email;
        }
        finally { _gate.Release(); }
    }

    static async Task Reply(HttpListenerContext ctx, bool ok)
    {
        var html = ok
            ? "<html><body dir='rtl' style='font-family:Segoe UI;text-align:center;margin-top:20vh'><h2>روزنما به تقویم گوگل وصل شد</h2><p>Rooznama is connected. You can close this tab.</p></body></html>"
            : "<html><body dir='rtl' style='font-family:Segoe UI;text-align:center;margin-top:20vh'><h2>اتصال انجام نشد</h2><p>The connection was not completed. You can close this tab.</p></body></html>";
        var bytes = Encoding.UTF8.GetBytes(html);
        ctx.Response.ContentType = "text/html; charset=utf-8";
        ctx.Response.ContentLength64 = bytes.Length;
        await ctx.Response.OutputStream.WriteAsync(bytes).ConfigureAwait(false);
        ctx.Response.Close();
    }

    static int FreePort()
    {
        var l = new TcpListener(IPAddress.Loopback, 0);
        l.Start();
        int port = ((IPEndPoint)l.LocalEndpoint).Port;
        l.Stop();
        return port;
    }

    // ---------- tokens ----------
    /// <summary>A valid access token; refreshed when it is about to expire (or when <paramref name="forceRefresh"/> says it was refused).</summary>
    public async Task<string> GetAccessTokenAsync(bool forceRefresh, CancellationToken ct)
    {
        await _gate.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_stored is null) throw new GoogleAuthException("not connected to Google");
            if (!forceRefresh && _access is not null && DateTime.UtcNow < _accessExpiresUtc.AddMinutes(-1)) return _access;

            var (clientId, clientSecret) = _client();
            var t = await PostAsync(TokenUrl, Form(clientId, clientSecret, new()
            {
                ["refresh_token"] = _stored.RefreshToken, ["grant_type"] = "refresh_token"
            }), ct, throwOnError: false).ConfigureAwait(false);

            if (t.Error == "invalid_grant")
            {
                // the access was withdrawn at Google (or the refresh token expired): forget it, the person has to connect again
                ForgetLocked();
                throw new GoogleAuthException("the access to Google was withdrawn; connect again");
            }
            if (t.AccessToken is null) throw new GoogleAuthException("Google refused to renew the access: " + (t.ErrorDescription ?? t.Error));

            _access = t.AccessToken;
            _accessExpiresUtc = DateTime.UtcNow.AddSeconds(Math.Max(60, t.ExpiresIn));
            return _access;
        }
        finally { _gate.Release(); }
    }

    public async Task DisconnectAsync()
    {
        string? token;
        await _gate.WaitAsync().ConfigureAwait(false);
        try
        {
            token = _stored?.RefreshToken;
            ForgetLocked();
        }
        finally { _gate.Release(); }

        if (token is null) return;
        try   // best effort: the access disappears from the Google account's list of connected apps
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, RevokeUrl) { Content = new FormUrlEncodedContent(new Dictionary<string, string> { ["token"] = token }) };
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var resp = await _http.SendAsync(req, cts.Token).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException) { }
    }

    void ForgetLocked()
    {
        _stored = null;
        _access = null;
        try { File.Delete(_tokenFile); } catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
    }

    async Task<TokenResponse> PostAsync(string url, Dictionary<string, string> form, CancellationToken ct, bool throwOnError = true)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, url) { Content = new FormUrlEncodedContent(form) };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
        var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        TokenResponse t;
        try { t = JsonSerializer.Deserialize<TokenResponse>(text) ?? new TokenResponse(); }
        catch (JsonException) { t = new TokenResponse { Error = "bad_response", ErrorDescription = text.Length > 200 ? text[..200] : text }; }
        if (!resp.IsSuccessStatusCode && throwOnError)
            throw new GoogleAuthException($"Google refused: {t.ErrorDescription ?? t.Error ?? resp.StatusCode.ToString()}");
        return t;
    }

    // ---------- storage ----------
    Stored? ReadFile()
    {
        try
        {
            if (!File.Exists(_tokenFile)) return null;
            var plain = ProtectedData.Unprotect(File.ReadAllBytes(_tokenFile), null, DataProtectionScope.CurrentUser);
            var s = JsonSerializer.Deserialize<Stored>(plain);
            return s is { RefreshToken.Length: > 0 } ? s : null;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or CryptographicException or JsonException)
        {
            Log.Write("google: cannot read the stored sign-in: " + ex.Message);
            return null;
        }
    }

    void WriteFile(Stored s)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_tokenFile)!);
        var plain = JsonSerializer.SerializeToUtf8Bytes(s);
        File.WriteAllBytes(_tokenFile, ProtectedData.Protect(plain, null, DataProtectionScope.CurrentUser));
    }

    // ---------- helpers ----------
    // a desktop client normally has a secret, which Google asks for even with PKCE; an empty one is left out
    static Dictionary<string, string> Form(string clientId, string clientSecret, Dictionary<string, string> rest)
    {
        rest["client_id"] = clientId;
        if (clientSecret.Length > 0) rest["client_secret"] = clientSecret;
        return rest;
    }

    static string Base64Url(byte[] b) => Convert.ToBase64String(b).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    // the id_token is only read for the e-mail address shown to the person; it is not used to authorize anything
    static string? EmailFrom(string? idToken)
    {
        try
        {
            var parts = idToken?.Split('.');
            if (parts is not { Length: 3 }) return null;
            var p = parts[1].Replace('-', '+').Replace('_', '/');
            p = p.PadRight(p.Length + (4 - p.Length % 4) % 4, '=');
            using var doc = JsonDocument.Parse(Convert.FromBase64String(p));
            return doc.RootElement.TryGetProperty("email", out var e) ? e.GetString() : null;
        }
        catch (Exception ex) when (ex is FormatException or JsonException) { return null; }
    }
}
