using AkGaming.Tournaments.Contracts.DTOs;
using AkGaming.Tournaments.Frontend.Api;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace AkGaming.Tournaments.Frontend.Components.Shared;

public partial class LogoUploadDialog : ComponentBase
{
    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private enum LogoFitMode
    {
        CropCenter,
        ContainFill
    }

    [Parameter] public EventCallback<MediaAssetDto> Uploaded { get; set; }

    [Inject] private MediaAssetsApiClient MediaAssetsClient { get; set; } = default!;

    private byte[]? sourceImageBytes;
    private string? sourceImageDataUrl;
    private string? sourceContentType;
    private string? sourceFileName;
    private string? errorMessage;
    private string? statusMessage;
    private LogoFitMode logoFitMode = LogoFitMode.CropCenter;
    private bool isOpen;
    private bool isBusy;

    private void OpenDialog()
        => isOpen = true;

    private void CloseDialog()
    {
        isOpen = false;
        ResetSelection();
    }

    private async Task HandleFileSelectedAsync(InputFileChangeEventArgs args)
    {
        var file = args.File;
        sourceContentType = file.ContentType;
        sourceFileName = file.Name;

        await using var stream = file.OpenReadStream(MaxUploadBytes);
        using var memory = new MemoryStream();
        await stream.CopyToAsync(memory);
        sourceImageBytes = memory.ToArray();
        sourceImageDataUrl = $"data:{file.ContentType};base64,{Convert.ToBase64String(sourceImageBytes)}";
        logoFitMode = LogoFitMode.CropCenter;
    }

    private async Task UploadAsync()
    {
        if (sourceImageBytes is null || string.IsNullOrWhiteSpace(sourceContentType))
            return;

        isBusy = true;
        errorMessage = null;
        statusMessage = "Uploading logo.";

        try
        {
            var asset = await MediaAssetsClient.UploadLogoAsync(sourceImageBytes, sourceFileName ?? "logo", sourceContentType, GetUploadFitMode());

            statusMessage = "Applying logo.";
            if (Uploaded.HasDelegate)
                await Uploaded.InvokeAsync(asset);

            CloseDialog();
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
        }
        finally
        {
            statusMessage = null;
            isBusy = false;
        }
    }

    private void ResetSelection()
    {
        sourceImageBytes = null;
        sourceImageDataUrl = null;
        sourceContentType = null;
        sourceFileName = null;
        errorMessage = null;
        statusMessage = null;
        logoFitMode = LogoFitMode.CropCenter;
    }

    private void SetFitMode(LogoFitMode fitMode)
        => logoFitMode = fitMode;

    private string GetPreviewStyle()
        => $"--logo-image: url('{sourceImageDataUrl}');";

    private string GetUploadFitMode()
        => logoFitMode switch
        {
            LogoFitMode.ContainFill => "contain-fill",
            _ => "crop-center"
        };
}
