using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AKG.Common.Generics;
using Frontend.Blazor.Handlers;
using Microsoft.AspNetCore.Mvc; // for ProblemDetails

namespace Frontend.Blazor.ApiClients;

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

    private static async Task<string> ReadError(HttpResponseMessage resp, CancellationToken ct) {
        try {
            // Prefer RFC7807 ProblemDetails if present
            var problem = await resp.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: ct);
            if (problem is not null) {
                var detail = string.IsNullOrWhiteSpace(problem.Detail) ? problem.Title : problem.Detail;
                return $"{(int)resp.StatusCode} {resp.StatusCode}: {detail}";
            }
        } catch (Exception e) {
            return "Error reading response body. " + e.Message;
        }

        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!string.IsNullOrWhiteSpace(text)) return $"{(int)resp.StatusCode} {resp.StatusCode}: {text}";
        var reason = resp.ReasonPhrase ?? resp.StatusCode.ToString();
        return $"{(int)resp.StatusCode} {reason}";
    }
}
