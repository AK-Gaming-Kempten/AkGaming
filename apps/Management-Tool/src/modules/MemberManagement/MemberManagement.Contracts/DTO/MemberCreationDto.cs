namespace MemberManagement.Contracts.DTO;

/// <summary>
/// Dto responsible for transferring member creation data
/// </summary>
public class MemberCreationDto(
    string? firstName,
    string? lastName,
    string? email,
    string? phone,
    string? discordUsername,
    DateTime birthDate,
    AddressDto address)
{
    public string? FirstName { get; set; } = firstName;
    public string? LastName { get; set; } = lastName;
    public string? Email { get; set; } = email;
    public string? Phone { get; set; } = phone;
    public string? DiscordUsername { get; set; } = discordUsername;
    public DateTime BirthDate { get; set; } = birthDate;
    public AddressDto Address { get; set; } = address;
}