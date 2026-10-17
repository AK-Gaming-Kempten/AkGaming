using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
namespace AkGaming.Gamenight.Shared;

public interface IUserSession
{
    Task<ClaimsPrincipal> UserAsync();
    Task<string?> TokenAsync();
    Task LoginAsync();
    Task LogoutAsync();
}
public interface IRegistrationExport
{
    Task SaveAsync(string name, string csv);
}
public sealed class GamenightClient(HttpClient http, IUserSession session)
{
    public async Task<T> SendAsync<T>(HttpMethod method, string path, object? body = null)
    {
        using var request = new HttpRequestMessage(method, path);
        if (body is not null) request.Content = JsonContent.Create(body);
        var token = await session.TokenAsync();
        if (token is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await http.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var raw = await response.Content.ReadAsStringAsync();
            string? title = null;
            try { using var json = JsonDocument.Parse(raw); if (json.RootElement.TryGetProperty("title", out var value)) title = value.GetString(); } catch (JsonException) { }
            throw new HttpRequestException(title ?? $"Anfrage fehlgeschlagen ({(int)response.StatusCode}). Bitte neu laden oder erneut anmelden.");
        }
        if (typeof(T) == typeof(string)) return (T)(object)await response.Content.ReadAsStringAsync();
        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return default!;
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }
}
