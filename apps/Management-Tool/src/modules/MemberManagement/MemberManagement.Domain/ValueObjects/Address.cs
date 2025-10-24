namespace MemberManagement.Domain.ValueObjects;

public class Address {
    public string? Street { get; set; }
    public string? ZipCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    
    public Address() {
        Street = string.Empty;
        ZipCode = string.Empty;
        City = string.Empty;
        Country = string.Empty;
    }
    
    public Address(
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