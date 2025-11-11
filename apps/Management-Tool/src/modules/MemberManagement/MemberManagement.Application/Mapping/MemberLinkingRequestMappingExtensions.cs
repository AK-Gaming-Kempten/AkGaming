using MemberManagement.Contracts.DTO;
using MemberManagement.Domain.Entities;
using DomainEnums = MemberManagement.Domain.Enums;
using ContractEnums = MemberManagement.Contracts.Enums;

namespace MemberManagement.Application.Mapping;

public static class MemberLinkingRequestMappingExtensions {
    public static MemberLinkingRequest ToMemberLinkingRequest(this MemberLinkingRequestDto dto) => new() {
        Id = dto.Id,
        IssuingUserId = dto.IssuingUserId,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        DiscordUserName = dto.DiscordUserName,
        Reason =  (DomainEnums.MemberLinkingRequestReason)dto.Reason,
        IsResolved = dto.IsResolved
    };
    public static MemberLinkingRequestDto ToMemberLinkingRequestDto(this MemberLinkingRequest request) => new() {
        Id = request.Id,
        IssuingUserId = request.IssuingUserId,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        DiscordUserName = request.DiscordUserName,
        Reason =  (ContractEnums.MemberLinkingRequestReason)request.Reason,
        IsResolved = request.IsResolved
    };
}