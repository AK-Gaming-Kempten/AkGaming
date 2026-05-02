using System.Net.Http.Json;
using System.Text.Json;

namespace AkGaming.Tournaments.Frontend.Api;

public abstract class TournamentApiClientBase(HttpClient httpClient)
{
    protected async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default, bool authorize = true)
    {
        try
        {
            using var request = CreateGetRequest(uri, authorize);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task<T> PostAsync<T>(string uri, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(uri, request, TournamentApiJson.Options, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task<T> PutAsync<T>(string uri, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PutAsJsonAsync(uri, request, TournamentApiJson.Options, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task PutAsync(string uri, object request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PutAsJsonAsync(uri, request, TournamentApiJson.Options, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response, cancellationToken);
                throw new TournamentApiException(response.StatusCode, message);
            }
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task<T> PostMultipartAsync<T>(string uri, MultipartFormDataContent request, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.PostAsync(uri, request, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task<T?> GetOrDefaultAsync<T>(string uri, CancellationToken cancellationToken = default, bool authorize = true)
    {
        try
        {
            using var request = CreateGetRequest(uri, authorize);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
                return default;

            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task DeleteAsync(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.DeleteAsync(uri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var message = await ReadErrorMessageAsync(response, cancellationToken);
                throw new TournamentApiException(response.StatusCode, message);
            }
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    protected async Task<T> DeleteAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.DeleteAsync(uri, cancellationToken);
            return await ReadResponseAsync<T>(response, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw CreateConnectionException(ex);
        }
    }

    private static async Task<T> ReadResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            var message = await ReadErrorMessageAsync(response, cancellationToken);
            throw new TournamentApiException(response.StatusCode, message);
        }

        var result = await response.Content.ReadFromJsonAsync<T>(TournamentApiJson.Options, cancellationToken);
        return result ?? throw new TournamentApiException(response.StatusCode, "The tournament API returned an empty response.");
    }

    private static async Task<string> ReadErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!string.IsNullOrWhiteSpace(content))
        {
            try
            {
                using var json = JsonDocument.Parse(content);
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                {
                    if (json.RootElement.TryGetProperty("detail", out var detailElement)
                        && detailElement.ValueKind == JsonValueKind.String)
                    {
                        var detail = detailElement.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                        {
                            return detail;
                        }
                    }

                    if (json.RootElement.TryGetProperty("title", out var titleElement)
                        && titleElement.ValueKind == JsonValueKind.String)
                    {
                        var title = titleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                        {
                            return title;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Fall through to plain-text response body.
            }

            return content;
        }

        return $"The tournament API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
    }

    private static TournamentApiException CreateConnectionException(HttpRequestException exception)
        => new("The tournament API could not be reached. Check that the backend is running and that the configured API URL is correct.", exception);

    private static HttpRequestMessage CreateGetRequest(string uri, bool authorize)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, uri);
        if (!authorize)
            request.Options.Set(TournamentApiAuthorizationHandler.SkipAuthorizationOptionKey, true);

        return request;
    }
}