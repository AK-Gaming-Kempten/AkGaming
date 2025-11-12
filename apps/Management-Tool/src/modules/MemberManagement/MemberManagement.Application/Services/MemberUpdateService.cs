using AKG.Common.Extensions;
using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.ValueObjects;

namespace MemberManagement.Application.Services;

public class MemberUpdateService : IMemberUpdateService {
    
    private readonly IMemberRepository _memberRepository;
    public MemberUpdateService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result> UpdateMemberAsync(Guid memberId, MemberDto memberData) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(memberId);
        if (!memberResult.IsSuccess)
            return memberResult;
        var member = memberResult.Value!;

        member.FirstName = memberData.FirstName;
        member.LastName = memberData.LastName;
        member.Email = memberData.Email;
        member.PhoneNumber = memberData.Phone;
        member.DiscordUsername = memberData.DiscordUserName;
        member.BirthDate = memberData.BirthDate;
        member.Address = new Address()
        {
            Street = memberData.Address.Street,
            ZipCode = memberData.Address.ZipCode,
            City = memberData.Address.City,
            Country = memberData.Address.Country
        };

        var result = await _memberRepository.Update(member)
            .Then(() => _memberRepository.SaveChangesAsync());
        
        return result;
    }
}