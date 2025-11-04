using AKG.Common.Generics;
using MemberManagement.Application.Interfaces;
using MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MemberManagement.Infrastructure.Persistence.Repositories;

public class EfMemberRepository : IMemberRepository {
    private readonly MemberManagementDbContext _dbContext;

    public EfMemberRepository(MemberManagementDbContext dbContext) {
        _dbContext = dbContext;
    }

    public async Task<Result<Member>> GetByMemberIdAsync(Guid id) {
        try {
            var member = await _dbContext.Members
                .Include(m => m.StatusChanges)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (member is null)
                return Result<Member>.Failure("Member not found.");

            return Result<Member>.Success(member);
        }
        catch (Exception ex) {
            return Result<Member>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<Member>> GetByUserIdAsync(Guid id) {
        try {
            var member = await _dbContext.Members
                .Include(m => m.StatusChanges)
                .FirstOrDefaultAsync(m => m.UserId == id);

            if (member is null)
                return Result<Member>.Failure("Member not found.");

            return Result<Member>.Success(member);
        }
        catch (Exception ex) {
            return Result<Member>.Failure($"Database error: {ex.Message}");
        }
    }

    public async Task<Result<List<Member>>> GetAllAsync() {
        try {
            var members = await _dbContext.Members
                .Include(m => m.StatusChanges)
                .ToListAsync();

            return Result<List<Member>>.Success(members);
        }
        catch (Exception ex) {
            return Result<List<Member>>.Failure($"Database error: {ex.Message}");
        }
    }

    public Task<Result<Guid>> AddAsync(Member member) {
        try {
            _dbContext.Members.Add(member);
            return Task.FromResult(Result<Guid>.Success(member.Id));
        }
        catch (Exception ex) {
            return Task.FromResult(Result<Guid>.Failure($"Failed to add member: {ex.Message}"));
        }
    }

    public Task<Result> UpdateAsync(Member member) {
        try {
            _dbContext.Members.Update(member);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex) {
            return Task.FromResult(Result.Failure($"Failed to update member: {ex.Message}"));
        }
    }

    public Task<Result> DeleteAsync(Member member) {
        try {
            _dbContext.Members.Remove(member);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex) {
            return Task.FromResult(Result.Failure($"Failed to delete member: {ex.Message}"));
        }
    }

    public async Task<Result> SaveChangesAsync() {
        try {
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to save changes: {ex.Message}");
        }
    }
}