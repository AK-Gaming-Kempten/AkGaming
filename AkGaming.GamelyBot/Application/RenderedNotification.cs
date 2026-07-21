namespace AkGaming.GamelyBot.Application;

public sealed record RenderedButton(string Label, string CustomId, int Style);

public sealed record RenderedMessage(string Title, string Body, string? Url = null, string? RoleId = null,
    string? ChannelId = null, IReadOnlyList<RenderedButton>? Buttons = null);

public sealed record RenderedNotification(RenderedMessage? ChannelMessage, RenderedMessage? DirectMessage);

public sealed record TransportResult(bool IsSuccess, bool IsPermanentFailure, string? ExternalMessageId = null, string? Error = null)
{
    public static TransportResult Success(string? externalMessageId) => new(true, false, externalMessageId);
    public static TransportResult TemporaryFailure(string error) => new(false, false, Error: error);
    public static TransportResult PermanentFailure(string error) => new(false, true, Error: error);
}
