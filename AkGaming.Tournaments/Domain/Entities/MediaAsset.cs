namespace AkGaming.Tournaments.Domain.Entities;

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = [];
    public long SizeBytes { get; set; }
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
