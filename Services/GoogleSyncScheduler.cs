using System.Net.Http;

namespace Rooznama.Services;

public enum GoogleSyncState { NotConnected, Idle, Syncing, Error, NeedsReconnect }

/// <summary>
/// Runs the Google sync: shortly after the app starts, every 15 minutes, a few seconds after the person edits an event, and on demand.
/// One sync at a time. Raises <see cref="StatusChanged"/> on a background thread.
/// </summary>
public sealed class GoogleSyncScheduler : IDisposable
{
    static readonly TimeSpan Every = TimeSpan.FromMinutes(15);
    static readonly TimeSpan AfterEdit = TimeSpan.FromSeconds(5);

    readonly GoogleAuth _auth;
    readonly PersonalEventStore _store;
    readonly SemaphoreSlim _one = new(1, 1);
    readonly HttpClient _http;
    Timer? _timer;
    Timer? _debounce;
    CancellationTokenSource? _cts;

    public GoogleSyncState State { get; private set; } = GoogleSyncState.NotConnected;
    public GoogleSyncResult? LastResult { get; private set; }
    public string? LastError { get; private set; }
    public DateTime? LastSyncUtc { get; private set; }

    public event Action? StatusChanged;

    public GoogleSyncScheduler(GoogleAuth auth, PersonalEventStore store, HttpClient http)
    {
        _auth = auth;
        _store = store;
        _http = http;
        State = auth.IsConnected ? GoogleSyncState.Idle : GoogleSyncState.NotConnected;
        LastSyncUtc = App.Settings.GoogleLastSyncUtc == default ? (DateTime?)null : App.Settings.GoogleLastSyncUtc;
    }

    /// <summary>Starts the timers (when connected and automatic sync is on). Safe to call again after connecting or changing the setting.</summary>
    public void Start()
    {
        Stop();
        if (!App.Settings.Experimental) return;   // experimental, off: no timers, no requests
        if (!_auth.IsConnected) { Set(GoogleSyncState.NotConnected); return; }
        if (State == GoogleSyncState.NotConnected || State == GoogleSyncState.NeedsReconnect) Set(GoogleSyncState.Idle);
        if (!App.Settings.GoogleAutoSync) return;

        _timer = new Timer(OnTick, null, TimeSpan.FromSeconds(10), Every);
        _store.Edited += OnEdited;
    }

    public void Stop()
    {
        _store.Edited -= OnEdited;
        _timer?.Dispose(); _timer = null;
        _debounce?.Dispose(); _debounce = null;
    }

    void OnEdited()
    {
        if (!_auth.IsConnected) return;
        _debounce?.Dispose();
        _debounce = new Timer(OnTick, null, AfterEdit, Timeout.InfiniteTimeSpan);
    }

    void OnTick(object? state) => _ = SyncNowAsync();

    /// <summary>Syncs now. Does nothing when a sync is already running or the account is not connected.</summary>
    public async Task SyncNowAsync()
    {
        if (!App.Settings.Experimental) return;
        if (!_auth.IsConnected) { Set(GoogleSyncState.NotConnected); return; }
        if (!await _one.WaitAsync(0).ConfigureAwait(false)) return;
        _cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        try
        {
            Set(GoogleSyncState.Syncing);
            var api = new GoogleCalendarApi(_http, _auth.GetAccessTokenAsync);
            var sync = new GoogleSyncService(api, _store, d => HijriService.Get(d), () => DateTime.Today, Log.Write,
                                             untitled: Loc.T("event.untitled"));
            var r = await sync.SyncAsync(_cts.Token).ConfigureAwait(false);

            LastResult = r;
            LastSyncUtc = DateTime.UtcNow;
            App.Settings.GoogleLastSyncUtc = LastSyncUtc.Value;
            App.Settings.Save();
            LastError = r.Failed > 0 ? r.FirstError : null;
            Log.Write($"google sync: sent {r.Uploaded}, deleted {r.DeletedRemotely}, imported {r.Imported}, updated {r.UpdatedLocally}, removed {r.RemovedLocally}, failed {r.Failed}");
            Set(r.Failed > 0 ? GoogleSyncState.Error : GoogleSyncState.Idle);
        }
        catch (GoogleAuthException ex)
        {
            LastError = ex.Message;
            Log.Write("google sync: " + ex.Message);
            Set(_auth.IsConnected ? GoogleSyncState.Error : GoogleSyncState.NeedsReconnect);
        }
        catch (Exception ex)   // network, Google's answer, a cancelled sync ...: shown in the settings, retried at the next run
        {
            LastError = ex.Message;
            Log.Write("google sync failed: " + ex.Message);
            Set(GoogleSyncState.Error);
        }
        finally
        {
            _cts?.Dispose(); _cts = null;
            _one.Release();
        }
    }

    void Set(GoogleSyncState s)
    {
        State = s;
        StatusChanged?.Invoke();
    }

    /// <summary>Called after a successful connect or a disconnect.</summary>
    public void Reset()
    {
        LastResult = null;
        LastError = null;
        LastSyncUtc = null;
        Start();
    }

    public void Dispose()
    {
        Stop();
        _cts?.Cancel();
    }
}
