namespace AkGaming.Tournaments.Domain.Entities;

public sealed class MediaAsset
{
    public Guid Id { get; set; }
    public string StorageKey { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public DateTimeOffset CreatedUtc { get; set; } = DateTimeOffset.UtcNow;
}
