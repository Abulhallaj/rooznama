using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Rooznama.Services;

/// <summary>What the sync needs from Google Calendar. The real one talks HTTP; the tests use an in-memory fake.</summary>
public interface IGoogleCalendarApi
{
    /// <summary>The events that overlap the window, repeating events expanded into their occurrences, deleted ones left out.</summary>
    Task<IReadOnlyList<GoogleEvent>> ListAsync(DateTimeOffset timeMin, DateTimeOffset timeMax, CancellationToken ct = default);
    Task<GoogleEvent> InsertAsync(GoogleEvent e, CancellationToken ct = default);
    /// <summary>Changes only the fields that are set (an empty string clears a text field).</summary>
    Task<GoogleEvent> PatchAsync(string id, GoogleEvent e, CancellationToken ct = default);
    /// <summary>An event that is already gone counts as deleted.</summary>
    Task DeleteAsync(string id, CancellationToken ct = default);
}

/// <summary>The sign-in is no longer usable (the access was revoked, or the client is not set up). The user has to connect again.</summary>
public sealed class GoogleAuthException : Exception
{
    public GoogleAuthException(string message) : base(message) { }
}

public sealed class GoogleApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public GoogleApiException(HttpStatusCode status, string body)
        : base($"Google Calendar answered {(int)status} {status}: {Shorten(body)}")
    {
        StatusCode = status;
    }

    static string Shorten(string s) => s.Length <= 300 ? s : s[..300] + "…";
}

/// <summary>Google Calendar API v3, events of one calendar (the primary one by default).</summary>
public sealed class GoogleCalendarApi : IGoogleCalendarApi
{
    const string Base = "https://www.googleapis.com/calendar/v3/calendars/";

    static readonly JsonSerializerOptions Json = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    readonly HttpClient _http;
    readonly Func<bool, CancellationToken, Task<string>> _accessToken;   // (forceRefresh, ct) -> a valid access token
    readonly Func<TimeSpan, CancellationToken, Task> _delay;
    readonly string _events;

    public GoogleCalendarApi(HttpClient http, Func<bool, CancellationToken, Task<string>> accessToken,
                             string calendarId = "primary", Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        _http = http;
        _accessToken = accessToken;
        _delay = delay ?? ((d, token) => Task.Delay(d, token));
        _events = Base + Uri.EscapeDataString(calendarId) + "/events";
    }

    public async Task<IReadOnlyList<GoogleEvent>> ListAsync(DateTimeOffset timeMin, DateTimeOffset timeMax, CancellationToken ct = default)
    {
        var all = new List<GoogleEvent>();
        string? page = null;
        do
        {
            var url = new StringBuilder(_events)
                .Append("?singleEvents=true&showDeleted=false&orderBy=startTime&maxResults=2500")
                .Append("&timeMin=").Append(Uri.EscapeDataString(Rfc3339(timeMin)))
                .Append("&timeMax=").Append(Uri.EscapeDataString(Rfc3339(timeMax)));
            if (page is not null) url.Append("&pageToken=").Append(Uri.EscapeDataString(page));

            var body = await SendAsync(HttpMethod.Get, url.ToString(), null, ct).ConfigureAwait(false);
            var list = JsonSerializer.Deserialize<EventList>(body, Json) ?? new EventList();
            if (list.Items is not null) all.AddRange(list.Items);
            page = string.IsNullOrEmpty(list.NextPageToken) ? null : list.NextPageToken;
        }
        while (page is not null);
        return all;
    }

    public async Task<GoogleEvent> InsertAsync(GoogleEvent e, CancellationToken ct = default) =>
        Parse(await SendAsync(HttpMethod.Post, _events, e, ct).ConfigureAwait(false));

    public async Task<GoogleEvent> PatchAsync(string id, GoogleEvent e, CancellationToken ct = default) =>
        Parse(await SendAsync(HttpMethod.Patch, _events + "/" + Uri.EscapeDataString(id), e, ct).ConfigureAwait(false));

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        try { await SendAsync(HttpMethod.Delete, _events + "/" + Uri.EscapeDataString(id), null, ct).ConfigureAwait(false); }
        catch (GoogleApiException ex) when (ex.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Gone) { }
    }

    static GoogleEvent Parse(string body) =>
        JsonSerializer.Deserialize<GoogleEvent>(body, Json) ?? throw new JsonException("Google sent an empty event");

    static string Rfc3339(DateTimeOffset t) => t.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>One request. A 401 gets one retry with a fresh token; rate limits and server errors get a few retries.</summary>
    async Task<string> SendAsync(HttpMethod method, string url, object? body, CancellationToken ct)
    {
        bool refreshed = false;
        for (int attempt = 0; ; attempt++)
        {
            using var req = new HttpRequestMessage(method, url);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await _accessToken(refreshed, ct).ConfigureAwait(false));
            if (body is not null)
                req.Content = new StringContent(JsonSerializer.Serialize(body, body.GetType(), Json), Encoding.UTF8, "application/json");

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var text = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode) return text;

            if (resp.StatusCode == HttpStatusCode.Unauthorized && !refreshed) { refreshed = true; continue; }

            bool transient = resp.StatusCode == (HttpStatusCode)429 || (int)resp.StatusCode >= 500
                             || (resp.StatusCode == HttpStatusCode.Forbidden && text.Contains("ateLimitExceeded", StringComparison.Ordinal));
            if (transient && attempt < 3)
            {
                await _delay(TimeSpan.FromSeconds(1 << attempt), ct).ConfigureAwait(false);
                continue;
            }
            throw new GoogleApiException(resp.StatusCode, text);
        }
    }

    sealed class EventList
    {
        [JsonPropertyName("items")] public List<GoogleEvent>? Items { get; set; }
        [JsonPropertyName("nextPageToken")] public string? NextPageToken { get; set; }
    }
}
