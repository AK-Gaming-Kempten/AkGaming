using MemberManagement.Contracts.DTO;
using MemberManagement.Domain.Entities;
using MemberManagement.Domain.ValueObjects;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Application.Mapping;

public static class MemberMappingExtensions {
    public static MemberDto ToDto(this Member m) => new() {
        Id = m.Id,
        UserId = m.UserId,
        FirstName = m.FirstName,
        LastName = m.LastName,
        Email = m.Email,
        Phone = m.PhoneNumber,
        DiscordUserName = m.DiscordUsername,
        BirthDate = m.BirthDate,
        Address = m.Address?.ToDto(),
        Status = (ContractEnums.MembershipStatus)m.Status,
        StatusChanges = m.StatusChanges.Select(sc => sc.ToDto()).ToList()
    };

    public static AddressDto ToDto(this Address a) => new() {
        Street = a.Street,
        ZipCode = a.ZipCode,
        City = a.City,
        Country = a.Country
    };

    public static MembershipStatusChangeEventDto ToDto(this MembershipStatusChangeEvent e) => new() {
        OldStatus = (ContractEnums.MembershipStatus)e.OldStatus,
        NewStatus = (ContractEnums.MembershipStatus)e.NewStatus,
        Timestamp = e.Timestamp
    };
}
