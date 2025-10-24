using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.DTO;
using MemberManagement.Contracts.Services;
using MemberManagement.Domain.Entities;
using MemberManagement.Domain.ValueObjects;
using UserManagement.Contracts.Services;

namespace MemberManagement.Application.Services;

public class MemberCreationService : IMemberCreationService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberCreationService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    /// <inheritdoc/>
    public async Task<Result> CreateMemberAsync(MemberCreationDto memberCreationData) {
        var member = new Member();
        
        member.FirstName = memberCreationData.FirstName;
        member.LastName = memberCreationData.LastName;
        member.Email = memberCreationData.Email;
        member.PhoneNumber = memberCreationData.Phone;
        member.DiscordUsername = memberCreationData.DiscordUsername;
        member.BirthDate = memberCreationData.BirthDate;
        member.Address = new Address(
            memberCreationData.Address.Street,
            memberCreationData.Address.ZipCode,
            memberCreationData.Address.City,
            memberCreationData.Address.Country
        );
        
        await _memberRepository.AddAsync(member);
        await _memberRepository.SaveChangesAsync();
        return Result.Success();
    }
}