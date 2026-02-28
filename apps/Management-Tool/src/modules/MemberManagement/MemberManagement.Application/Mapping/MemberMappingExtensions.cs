using MemberManagement.Contracts.DTO;
using MemberManagement.Domain.Entities;
using MemberManagement.Domain.ValueObjects;
using ContractEnums = MemberManagement.Contracts.Enums;
using DomainEnums = MemberManagement.Domain.Enums;

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
        Timestamp = e.Timestamp.ToUniversalTime()
    };
    
    public static Member ToMember(this MemberDto dto) => new() {
        Id = dto.Id,
        UserId = dto.UserId,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        PhoneNumber = dto.Phone,
        DiscordUsername = dto.DiscordUserName,
        BirthDate = dto.BirthDate,
        Address = dto.Address?.ToAddress(),
        Status = (DomainEnums.MembershipStatus)dto.Status,
        StatusChanges = dto.StatusChanges.Select(sc => sc.ToMembershipStatusChangeEvent()).ToList()
    };
    
    public static Address ToAddress(this AddressDto dto) => new() {
        Street = dto.Street,
        ZipCode = dto.ZipCode,
        City = dto.City,
        Country = dto.Country
    };
    
    public static MembershipStatusChangeEvent ToMembershipStatusChangeEvent(this MembershipStatusChangeEventDto dto) => new() {
        OldStatus = (DomainEnums.MembershipStatus)dto.OldStatus,
        NewStatus = (DomainEnums.MembershipStatus)dto.NewStatus,
        Timestamp = dto.Timestamp.ToUniversalTime()
    };
    
    public static Member ToMember(this MemberCreationDto dto) => new() {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        PhoneNumber = dto.Phone,
        DiscordUsername = dto.DiscordUserName,
        BirthDate = dto.BirthDate,
        Address = dto.Address?.ToAddress()
    };
    
    public static void UpdateMemberFromDto(this Member member, MemberDto dto) {
        member.FirstName = dto.FirstName;
        member.LastName = dto.LastName;
        member.Email = dto.Email;
        member.PhoneNumber = dto.Phone;
        member.DiscordUsername = dto.DiscordUserName;
        member.BirthDate = dto.BirthDate;
        member.Address = dto.Address?.ToAddress();
    }
}
