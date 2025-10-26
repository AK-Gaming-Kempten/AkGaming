using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using Membermanagement.Contracts.DTO;
using Membermanagement.Contracts.Services;
using ContractEnums = Membermanagement.Contracts.Enums ; 
using DomainEnums = MemberManagement.Domain.Enums;

namespace MemberManagement.Application.Services;

public class MemberQueryService : IMemberQueryService {
    private readonly IMemberRepository _memberRepository;
    
    public MemberQueryService(IMemberRepository memberRepository) {
        _memberRepository = memberRepository;
    }
    
    public async Task<Result<MemberDto>> GetMemberByGuidAsync(Guid id) {
        var memberResult = await _memberRepository.GetByMemberIdAsync(id);
        if (!memberResult.IsSuccess)
            return Result<MemberDto>.Failure(memberResult.Error ?? "Member not found");
        var member = memberResult.Value!;
        
        return Result<MemberDto>.Success(new MemberDto {
            Id = member.Id,
            UserId = member.UserId,
            FirstName = member.FirstName,
            LastName = member.LastName,
            Email = member.Email,
            Phone = member.PhoneNumber,
            DiscordUserName = member.DiscordUsername,
            BirthDate = member.BirthDate,
            Address = new AddressDto {
                Street = member.Address?.Street,
                ZipCode = member.Address?.ZipCode,
                City = member.Address?.City,
                Country = member.Address?.Country
            },
            Status = (ContractEnums.MembershipStatus)member.Status,
            StatusChanges = member.StatusChanges.Select(s => new MembershipStatusChangeEventDto {
                OldStatus = (ContractEnums.MembershipStatus)s.OldStatus,
                NewStatus = (ContractEnums.MembershipStatus)s.NewStatus,
                Timestamp = s.Timestamp
            }).ToList()
        });
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetAllMembersAsync() {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members.Select(m => new MemberDto {
            Id = m.Id,
            UserId = m.UserId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.PhoneNumber,
            DiscordUserName = m.DiscordUsername,
            BirthDate = m.BirthDate,
            Address = new AddressDto {
                Street = m.Address?.Street,
                ZipCode = m.Address?.ZipCode,
                City = m.Address?.City,
                Country = m.Address?.Country
            },
            Status = (ContractEnums.MembershipStatus)m.Status,
            StatusChanges = m.StatusChanges.Select(s => new MembershipStatusChangeEventDto {
                OldStatus = (ContractEnums.MembershipStatus)s.OldStatus,
                NewStatus = (ContractEnums.MembershipStatus)s.NewStatus,
                Timestamp = s.Timestamp
            }).ToList()
        }).ToList());
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ContractEnums.MembershipStatus status) {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members
            .Where(m => m.Status == (DomainEnums.MembershipStatus)status)
            .Select(m => new MemberDto {
            Id = m.Id,
            UserId = m.UserId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.PhoneNumber,
            DiscordUserName = m.DiscordUsername,
            BirthDate = m.BirthDate,
            Address = new AddressDto {
                Street = m.Address?.Street,
                ZipCode = m.Address?.ZipCode,
                City = m.Address?.City,
                Country = m.Address?.Country
            },
            Status = (ContractEnums.MembershipStatus)m.Status,
            StatusChanges = m.StatusChanges.Select(s => new MembershipStatusChangeEventDto {
                OldStatus = (ContractEnums.MembershipStatus)s.OldStatus,
                NewStatus = (ContractEnums.MembershipStatus)s.NewStatus,
                Timestamp = s.Timestamp
            }).ToList()
        }).ToList());
    }
    
    public async Task<Result<ICollection<MemberDto>>> GetMembersWithStatusAsync(ICollection<ContractEnums.MembershipStatus> statuses) {
        var membersResult = await _memberRepository.GetAllAsync();
        if (!membersResult.IsSuccess)
            return Result<ICollection<MemberDto>>.Failure(membersResult.Error ?? "Members not found");
        var members = membersResult.Value!;
        
        return Result<ICollection<MemberDto>>.Success(members
            .Where(m => statuses.Contains((ContractEnums.MembershipStatus)m.Status))
            .Select(m => new MemberDto {
            Id = m.Id,
            UserId = m.UserId,
            FirstName = m.FirstName,
            LastName = m.LastName,
            Email = m.Email,
            Phone = m.PhoneNumber,
            DiscordUserName = m.DiscordUsername,
            BirthDate = m.BirthDate,
            Address = new AddressDto {
                Street = m.Address?.Street,
                ZipCode = m.Address?.ZipCode,
                City = m.Address?.City,
                Country = m.Address?.Country
            },
            Status = (ContractEnums.MembershipStatus)m.Status,
            StatusChanges = m.StatusChanges.Select(s => new MembershipStatusChangeEventDto {
                OldStatus = (ContractEnums.MembershipStatus)s.OldStatus,
                NewStatus = (ContractEnums.MembershipStatus)s.NewStatus,
                Timestamp = s.Timestamp
            }).ToList()
        }).ToList());
    }
}