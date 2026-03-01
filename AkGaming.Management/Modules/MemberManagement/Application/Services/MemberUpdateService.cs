using AkGaming.Core.Common.Extensions;
using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Application.Mapping;
using AkGaming.Management.Modules.MemberManagement.Contracts.DTO;
using AkGaming.Management.Modules.MemberManagement.Contracts.Services;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using System.Text.Json;

namespace AkGaming.Management.Modules.MemberManagement.Application.Services;

public class MemberUpdateService : IMemberUpdateService {
    
    private readonly IMemberRepository _memberRepository;
    private readonly IMemberAuditLogWriter _auditLogWriter;
    
    public MemberUpdateService(IMemberRepository memberRepository, IMemberAuditLogWriter auditLogWriter) {
        _memberRepository = memberRepository;
        _auditLogWriter = auditLogWriter;
    }
    
    /// <inheritdoc/>
    public async Task<Result> UpdateMemberAsync(Guid memberId, MemberDto memberData, Guid? performedByUserId = null) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;

        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        CaptureMemberChanges(member, memberData, oldValues, newValues);

        member.UpdateMemberFromDto(memberData);

        var result = await _memberRepository.Update(member).Then(() => {
            if (oldValues.Count == 0) {
                return Result.Success();
            }

            var auditResult = _auditLogWriter.Add(new MemberAuditLog {
                ActionType = "MemberDetailsUpdated",
                PerformedByUserId = performedByUserId,
                EntityType = nameof(Member),
                EntityId = member.Id,
                OldValuesJson = JsonSerializer.Serialize(oldValues),
                NewValuesJson = JsonSerializer.Serialize(newValues)
            });

            return auditResult;
        }).Then(() => _memberRepository.SaveChangesAsync());
        
        return result;
    }

    private static void CaptureMemberChanges(
        Member member,
        MemberDto memberData,
        IDictionary<string, object?> oldValues,
        IDictionary<string, object?> newValues)
    {
        AddIfChanged(oldValues, newValues, nameof(Member.FirstName), member.FirstName, memberData.FirstName);
        AddIfChanged(oldValues, newValues, nameof(Member.LastName), member.LastName, memberData.LastName);
        AddIfChanged(oldValues, newValues, nameof(Member.Email), member.Email, memberData.Email);
        AddIfChanged(oldValues, newValues, nameof(Member.PhoneNumber), member.PhoneNumber, memberData.Phone);
        AddIfChanged(oldValues, newValues, nameof(Member.DiscordUsername), member.DiscordUsername, memberData.DiscordUserName);
        AddIfChanged(oldValues, newValues, nameof(Member.BirthDate), member.BirthDate, memberData.BirthDate);

        var currentAddress = member.Address;
        var updatedAddress = memberData.Address;

        AddIfChanged(oldValues, newValues, "Address.Street", currentAddress?.Street, updatedAddress?.Street);
        AddIfChanged(oldValues, newValues, "Address.ZipCode", currentAddress?.ZipCode, updatedAddress?.ZipCode);
        AddIfChanged(oldValues, newValues, "Address.City", currentAddress?.City, updatedAddress?.City);
        AddIfChanged(oldValues, newValues, "Address.Country", currentAddress?.Country, updatedAddress?.Country);
    }

    private static void AddIfChanged(
        IDictionary<string, object?> oldValues,
        IDictionary<string, object?> newValues,
        string fieldName,
        object? oldValue,
        object? newValue)
    {
        if (Equals(oldValue, newValue)) {
            return;
        }

        oldValues[fieldName] = oldValue;
        newValues[fieldName] = newValue;
    }
}
