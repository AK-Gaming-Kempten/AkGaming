using AkGaming.Tournaments.Contracts.DTOs;

namespace AkGaming.Tournaments.Frontend.Api;

public sealed class MediaAssetsApiClient(HttpClient httpClient) : TournamentApiClientBase(httpClient)
{
    public async Task<MediaAssetDto> UploadLogoAsync(byte[] content, string fileName, string contentType, string fitMode, CancellationToken cancellationToken = default)
    {
        using var multipart = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(content);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        multipart.Add(fileContent, "file", fileName);
        multipart.Add(new StringContent(fitMode), "fitMode");
        return await PostMultipartAsync<MediaAssetDto>("api/media-assets/logos", multipart, cancellationToken);
    }
}
