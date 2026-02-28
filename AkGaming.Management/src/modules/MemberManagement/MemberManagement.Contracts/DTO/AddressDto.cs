namespace MemberManagement.Contracts.DTO;

/// <summary>
/// Mirror of <see cref="MemberManagement.Domain.Entities.Address"/> used for data transmission
/// </summary>
public class AddressDto {
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    
    public AddressDto() { }
    public AddressDto(
        string? street,
        string? zipCode,
        string? city,
        string? country
    ) {
        Street = street;
        ZipCode = zipCode;
        City = city;
        Country = country;
    }
}