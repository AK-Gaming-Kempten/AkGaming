using MemberManagement.Contracts.DTO;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Mapping;

public static class MembershipApplicationRequestMappingExtensions {
    public static MembershipApplicationRequest ToMembershipApplicationRequest(this MembershipApplicationRequestDto dto) => new() {
        Id = dto.Id,
        IssuingUserId = dto.IssuingUserId,
        FirstName = dto.MemberCreationInfo.FirstName,
        LastName = dto.MemberCreationInfo.LastName,
        Email = dto.MemberCreationInfo.Email,
        Phone = dto.MemberCreationInfo.Phone,
        DiscordUserName = dto.MemberCreationInfo.DiscordUserName,
        BirthDate = dto.MemberCreationInfo.BirthDate,
        Address = (dto.MemberCreationInfo.Address ?? new AddressDto()).ToAddress(),
        ApplicationText = dto.ApplicationText,
        IsResolved = dto.IsResolved
    };
    
    public static MembershipApplicationRequestDto ToDto(this MembershipApplicationRequest request) => new() {
        Id = request.Id,
        IssuingUserId = request.IssuingUserId,
        MemberCreationInfo = new MemberCreationDto() {
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            Phone = request.Phone,
            DiscordUserName = request.DiscordUserName,
            BirthDate = request.BirthDate,
            Address = request.Address?.ToDto()
        },
        ApplicationText = request.ApplicationText,
        IsResolved = request.IsResolved
    };
}