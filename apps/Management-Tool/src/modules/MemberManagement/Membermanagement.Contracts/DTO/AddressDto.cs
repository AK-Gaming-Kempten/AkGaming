namespace Membermanagement.Contracts.DTO;

/// <summary>
/// Mirror of <see cref="MemberManagement.Domain.Entities.Address"/> used for data transmission
/// </summary>
public class AddressDto {
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
}