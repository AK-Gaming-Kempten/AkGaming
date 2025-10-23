using MemberManagement.Application.Interfaces;
using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class MemberRepository : IMemberRepository {
    private readonly MemberManagementDbContext _dbContext;

    public MemberRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public Task<Member?> GetByIdAsync(Guid id) =>
        _dbContext.Members.FirstOrDefaultAsync(m => m.Id == id);

    public Task<List<Member>> GetAllAsync() =>
        _dbContext.Members.ToListAsync();

    public Task AddAsync(Member member) {
        _dbContext.Members.Add(member);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Member member) {
        _dbContext.Members.Update(member);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Member member) {
        _dbContext.Members.Remove(member);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _dbContext.SaveChangesAsync();
}