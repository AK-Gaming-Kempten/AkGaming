namespace MemberManagement.Contracts.DTO;

/// <summary>
/// Dto responsible for transferring member creation data
/// </summary>
public class MemberCreationDto()
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? DiscordUsername { get; set; }
    public DateOnly? BirthDate { get; set; }
    public AddressDto? Address { get; set; }
}