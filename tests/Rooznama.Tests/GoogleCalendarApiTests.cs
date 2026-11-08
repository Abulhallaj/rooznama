using System.Net;
using System.Text;
using Rooznama.Services;
using Xunit;

namespace Rooznama.Tests;

public sealed class GoogleCalendarApiTests
{
    sealed class FakeHandler : HttpMessageHandler
    {
        public readonly List<(HttpMethod Method, string Url, string? Body, string? Token)> Requests = new();
        public readonly Queue<(HttpStatusCode Status, string Body)> Replies = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.Method, request.RequestUri!.AbsoluteUri, body, request.Headers.Authorization?.Parameter));
            var (status, text) = Replies.Dequeue();
            return new HttpResponseMessage(status) { Content = new StringContent(text, Encoding.UTF8, "application/json") };
        }
    }

    readonly FakeHandler _handler = new();
    readonly List<bool> _refreshFlags = new();
    readonly List<TimeSpan> _delays = new();

    GoogleCalendarApi Api() => new(new HttpClient(_handler),
        (refresh, _) => { _refreshFlags.Add(refresh); return Task.FromResult(refresh ? "fresh-token" : "token"); },
        delay: (d, _) => { _delays.Add(d); return Task.CompletedTask; });

    void Reply(HttpStatusCode status, string body = "{}") => _handler.Replies.Enqueue((status, body));

    [Fact]
    public async Task Listing_follows_the_pages_and_asks_for_occurrences_inside_the_window()
    {
        Reply(HttpStatusCode.OK, """{ "items": [ { "id": "a" } ], "nextPageToken": "p2" }""");
        Reply(HttpStatusCode.OK, """{ "items": [ { "id": "b" }, { "id": "c" } ] }""");

        var events = await Api().ListAsync(new DateTimeOffset(2025, 11, 5, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2028, 11, 5, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(new[] { "a", "b", "c" }, events.Select(e => e.Id));
        Assert.Equal(2, _handler.Requests.Count);
        var first = _handler.Requests[0].Url;
        Assert.StartsWith("https://www.googleapis.com/calendar/v3/calendars/primary/events?", first);
        Assert.Contains("singleEvents=true", first);
        Assert.Contains("showDeleted=false", first);
        Assert.Contains("timeMin=2025-11-05T00%3A00%3A00Z", first);
        Assert.Contains("timeMax=2028-11-05T00%3A00%3A00Z", first);
        Assert.Contains("pageToken=p2", _handler.Requests[1].Url);
        Assert.All(_handler.Requests, r => Assert.Equal("token", r.Token));
    }

    [Fact]
    public async Task Inserting_posts_the_event_without_empty_fields_and_returns_what_Google_made()
    {
        Reply(HttpStatusCode.OK, """{ "id": "g1", "etag": "\"e1\"", "summary": "الف" }""");

        var made = await Api().InsertAsync(new GoogleEvent { Summary = "الف", Start = new GoogleEventTime { Date = "2026-11-05" } });

        Assert.Equal("g1", made.Id);
        Assert.Equal("\"e1\"", made.ETag);
        var sent = _handler.Requests.Single();
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Contains("\"summary\"", sent.Body);
        Assert.DoesNotContain("etag", sent.Body);          // null fields are not sent
        Assert.DoesNotContain("description", sent.Body);
    }

    [Fact]
    public async Task Patching_uses_PATCH_on_the_event_url_and_escapes_the_id()
    {
        Reply(HttpStatusCode.OK, """{ "id": "a b" }""");
        await Api().PatchAsync("a b", new GoogleEvent { Summary = "x" });
        Assert.Equal(HttpMethod.Patch, _handler.Requests.Single().Method);
        Assert.EndsWith("/events/a%20b", _handler.Requests.Single().Url);
    }

    [Fact]
    public async Task A_refused_token_is_renewed_once_and_the_request_repeated()
    {
        Reply(HttpStatusCode.Unauthorized, """{ "error": { "code": 401 } }""");
        Reply(HttpStatusCode.OK, """{ "items": [] }""");

        await Api().ListAsync(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(1));

        Assert.Equal(new[] { false, true }, _refreshFlags);
        Assert.Equal(new[] { "token", "fresh-token" }, _handler.Requests.Select(r => r.Token));
    }

    [Fact]
    public async Task A_second_401_is_an_error()
    {
        Reply(HttpStatusCode.Unauthorized);
        Reply(HttpStatusCode.Unauthorized);
        var ex = await Assert.ThrowsAsync<GoogleApiException>(() => Api().DeleteAsync("x"));
        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
    }

    [Fact]
    public async Task Rate_limits_and_server_errors_are_retried_with_growing_pauses()
    {
        Reply((HttpStatusCode)429);
        Reply(HttpStatusCode.ServiceUnavailable);
        Reply(HttpStatusCode.Forbidden, """{ "error": { "errors": [ { "reason": "rateLimitExceeded" } ] } }""");
        Reply(HttpStatusCode.OK, """{ "id": "ok" }""");

        var made = await Api().InsertAsync(new GoogleEvent { Summary = "x" });

        Assert.Equal("ok", made.Id);
        Assert.Equal(new[] { TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4) }, _delays);
    }

    [Fact]
    public async Task Retrying_gives_up_after_a_few_attempts()
    {
        for (int i = 0; i < 4; i++) Reply(HttpStatusCode.InternalServerError, "boom");
        var ex = await Assert.ThrowsAsync<GoogleApiException>(() => Api().InsertAsync(new GoogleEvent()));
        Assert.Equal(HttpStatusCode.InternalServerError, ex.StatusCode);
        Assert.Equal(4, _handler.Requests.Count);
    }

    [Fact]
    public async Task Other_errors_are_not_retried()
    {
        Reply(HttpStatusCode.BadRequest, """{ "error": "bad" }""");
        await Assert.ThrowsAsync<GoogleApiException>(() => Api().InsertAsync(new GoogleEvent()));
        Assert.Single(_handler.Requests);
        Assert.Empty(_delays);
    }

    [Fact]
    public async Task Deleting_an_event_that_is_already_gone_is_fine()
    {
        Reply(HttpStatusCode.NotFound);
        Reply(HttpStatusCode.Gone);
        await Api().DeleteAsync("a");
        await Api().DeleteAsync("b");

        Reply(HttpStatusCode.Forbidden, """{ "error": "forbidden" }""");
        await Assert.ThrowsAsync<GoogleApiException>(() => Api().DeleteAsync("c"));
    }
}
