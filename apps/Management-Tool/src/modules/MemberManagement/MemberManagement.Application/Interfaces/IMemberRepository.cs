using AKG.Common.Generics;
using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

/// <summary>
/// Interface for the member repository
/// </summary>
public interface IMemberRepository {
    Task<Result<Member>> GetByMemberIdAsync(Guid id);
    Task<Result<Member>> GetByUserIdAsync(Guid id);
    Task<Result<List<Member>>> GetAllAsync();
    Task<Result<Guid>> AddAsync(Member member);
    Task<Result> UpdateAsync(Member member);
    Task<Result> DeleteAsync(Member member);
    Task<Result> SaveChangesAsync();
}