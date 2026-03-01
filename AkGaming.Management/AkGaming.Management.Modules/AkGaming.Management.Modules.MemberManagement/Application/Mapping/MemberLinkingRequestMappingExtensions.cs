using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using DomainEnums = AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using ContractEnums = AkGaming.Management.Modules.MemberManagement.Contracts.Enums;

namespace AkGaming.Management.Modules.MemberManagement.Application.Mapping;

public static class MemberLinkingRequestMappingExtensions {
    public static MemberLinkingRequest ToMemberLinkingRequest(this MemberLinkingRequestDto dto) => new() {
        Id = dto.Id,
        IssuingUserId = dto.IssuingUserId,
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        DiscordUserName = dto.DiscordUserName,
        Reason =  (DomainEnums.MemberLinkingRequestReason)dto.Reason,
        PrivacyPolicyAccepted = dto.PrivacyPolicyAccepted,
        IsResolved = dto.IsResolved
    };
    public static MemberLinkingRequestDto ToDto(this MemberLinkingRequest request) => new() {
        Id = request.Id,
        IssuingUserId = request.IssuingUserId,
        FirstName = request.FirstName,
        LastName = request.LastName,
        Email = request.Email,
        DiscordUserName = request.DiscordUserName,
        Reason =  (ContractEnums.MemberLinkingRequestReason)request.Reason,
        PrivacyPolicyAccepted = request.PrivacyPolicyAccepted,
        IsResolved = request.IsResolved
    };
}
