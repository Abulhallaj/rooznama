using System.Text.Json;

namespace Rooznama.Services;

/// <summary>Which Google OAuth client to sign in with: the one typed in the settings, otherwise google-client.json next to the exe.</summary>
public static class GoogleClientConfig
{
    public static (string Id, string Secret) Get()
    {
        var s = App.Settings;
        if (s.GoogleClientId.Trim().Length > 0) return (s.GoogleClientId.Trim(), s.GoogleClientSecret.Trim());
        return FromFile(Path.Combine(AppContext.BaseDirectory, "google-client.json"));
    }

    /// <summary>{ "clientId": "...", "clientSecret": "..." }; anything else gives no client.</summary>
    public static (string Id, string Secret) FromFile(string path)
    {
        try
        {
            if (!File.Exists(path)) return ("", "");
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            string Read(string name) => doc.RootElement.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString()!.Trim() : "";
            return (Read("clientId"), Read("clientSecret"));
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            Log.Write("google: cannot read google-client.json: " + ex.Message);
            return ("", "");
        }
    }
}
