using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Frontend.Handlers;
using Microsoft.AspNetCore.Mvc; // for ProblemDetails

namespace AkGaming.Management.Frontend.ApiClients;

public abstract class ApiClientBase {
    protected readonly HttpClient Http;
    protected readonly JsonSerializerOptions Json;

    protected ApiClientBase(HttpClient http, JsonSerializerOptions? jsonOptions = null) {
        Http = http;
        Json = jsonOptions ?? JsonDefaults.Options;
    }

    protected async Task<Result<T>> GetAsync<T>(string url, CancellationToken ct = default) {
        using var resp = await Http.GetAsync(url, ct);
        return await ToResult<T>(resp, ct);
    }

    protected async Task<Result> GetAsync(string url, CancellationToken ct = default) {
        using var resp = await Http.GetAsync(url, ct);
        return await ToResult(resp, ct);
    }

    protected async Task<Result<TOut>> PostJsonAsync<TIn, TOut>(string url, TIn body, CancellationToken ct = default) {
        using var resp = await Http.PostAsJsonAsync(url, body, Json, ct);
        return await ToResult<TOut>(resp, ct);
    }

    protected async Task<Result> PostJsonAsync<TIn>(string url, TIn body, CancellationToken ct = default) {
        using var resp = await Http.PostAsJsonAsync(url, body, Json, ct);
        return await ToResult(resp, ct);
    }

    protected async Task<Result<TOut>> PutJsonAsync<TIn, TOut>(string url, TIn body, CancellationToken ct = default) {
        using var resp = await Http.PutAsJsonAsync(url, body, Json, ct);
        return await ToResult<TOut>(resp, ct);
    }

    protected async Task<Result> PutJsonAsync<TIn>(string url, TIn body, CancellationToken ct = default) {
        using var resp = await Http.PutAsJsonAsync(url, body, Json, ct);
        return await ToResult(resp, ct);
    }

    protected async Task<Result> PutValueAsync<T>(string url, T value, CancellationToken ct = default) {
        var json = JsonSerializer.Serialize(value, Json);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var resp = await Http.PutAsync(url, content, ct);
        return await ToResult(resp, ct);
    }

    protected async Task<Result<T>> ToResult<T>(HttpResponseMessage resp, CancellationToken ct) {
        if (resp.IsSuccessStatusCode) {
            var payload = await resp.Content.ReadFromJsonAsync<T>(Json, ct);
            if (payload is null) return Result<T>.Failure("Empty response body.");
            return Result<T>.Success(payload);
        }
        return Result<T>.Failure(await ReadError(resp, ct));
    }

    protected async Task<Result> ToResult(HttpResponseMessage resp, CancellationToken ct) {
        if (resp.IsSuccessStatusCode) return Result.Success();
        return Result.Failure(await ReadError(resp, ct));
    }

    private async Task<string> ReadError(HttpResponseMessage resp, CancellationToken ct) {
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(text)) {
            try {
                var problem = JsonSerializer.Deserialize<ProblemDetails>(text, Json);
                if (problem is not null && (!string.IsNullOrWhiteSpace(problem.Detail) || !string.IsNullOrWhiteSpace(problem.Title))) {
                    var detail = string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title : problem.Detail;
                    return $"{(int)resp.StatusCode} {resp.StatusCode}: {detail}";
                }
            }
            catch (JsonException) {
                // Fall back to plain text or JSON-string parsing below.
            }

            try {
                var stringPayload = JsonSerializer.Deserialize<string>(text, Json);
                if (!string.IsNullOrWhiteSpace(stringPayload))
                    return $"{(int)resp.StatusCode} {resp.StatusCode}: {stringPayload}";
            }
            catch (JsonException) {
                // Not a JSON string, return the raw text body.
            }

            return $"{(int)resp.StatusCode} {resp.StatusCode}: {text}";
        }

        var reason = resp.ReasonPhrase ?? resp.StatusCode.ToString();
        return $"{(int)resp.StatusCode} {reason}";
    }
}
