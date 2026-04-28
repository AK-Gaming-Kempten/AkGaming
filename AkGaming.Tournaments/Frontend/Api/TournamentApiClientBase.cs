using System.Net.Http.Json;

namespace AkGaming.Tournaments.Frontend.Api;

public abstract class TournamentApiClientBase(HttpClient httpClient)
{
    protected async Task<T> GetAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken);
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

    protected async Task<T?> GetOrDefaultAsync<T>(string uri, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync(uri, cancellationToken);
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
            return content;

        return $"The tournament API returned {(int)response.StatusCode} {response.ReasonPhrase}.";
    }

    private static TournamentApiException CreateConnectionException(HttpRequestException exception)
        => new("The tournament API could not be reached. Check that the backend is running and that the configured API URL is correct.", exception);
}
