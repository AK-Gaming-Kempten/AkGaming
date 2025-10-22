using MemberManagement.Domain.Entities;

namespace MemberManagement.Application.Interfaces;

public interface IMemberRepository {
    Task<Member?> GetByIdAsync(Guid id);
    Task<List<Member>> GetAllAsync();
    Task AddAsync(Member member);
    Task UpdateAsync(Member member);
    Task DeleteAsync(Member member);
    Task SaveChangesAsync();
}