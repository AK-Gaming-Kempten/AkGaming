namespace UserManagement.Domain.Entities;

public class UserExternalLogin {
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Provider { get; set; } = null!; // e.g. "Discord"
    public string ProviderUserId { get; set; } = null!;
    public string? AccessToken { get; set; }
}