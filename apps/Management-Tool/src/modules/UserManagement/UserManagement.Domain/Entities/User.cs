namespace UserManagement.Domain.Entities;

public class User {
    public Guid Id { get; set; }

    // Basic identity info
    public string Email { get; set; }
    public string? PasswordHash { get; set; }

    // External login connections
    public ICollection<UserExternalLogin> ExternalLogins { get; set; } = new List<UserExternalLogin>();

    // Domain info
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}