using AkGaming.Core.Common.Generics;
using AkGaming.Management.Modules.MemberManagement.Application.Interfaces;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence.Repositories;

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
            members.Sort((m1, m2) => String.Compare(m1.LastName?.ToLower(), m2.LastName?.ToLower(), StringComparison.Ordinal));
            return Result<List<Member>>.Success(members);
        }
        catch (Exception ex) {
            return Result<List<Member>>.Failure($"Database error: {ex.Message}");
        }
    }

    public Result Add(Member member) {
        try {
            _dbContext.Members.Add(member);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to add member: {ex.Message}");
        }
    }

    public Result Update(Member member) {
        try {
            _dbContext.Members.Update(member);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to update member: {ex.Message}");
        }
    }

    public Result Delete(Member member) {
        try {
            _dbContext.Members.Remove(member);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to delete member: {ex.Message}");
        }
    }
    
    public Result TryDelete(Guid id) {
        try {
            var member = _dbContext.Members
                .FirstOrDefault(m => m.Id == id);

            if (member is null)
                return Result.Failure("Member not found.");

            _dbContext.Members.Remove(member);
            return Result.Success();
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to delete member: {ex.Message}");
        }
    }

    public async Task<Result> SaveChangesAsync() {
        try {
            await _dbContext.SaveChangesAsync();
            return Result.Success();
        }
        catch (DbUpdateConcurrencyException ex) {
            foreach (var entry in ex.Entries)
                Console.WriteLine($"[EF] Concurrency issue on {entry.Entity.GetType().Name} (state: {entry.State})");
            return Result.Failure($"Concurrency conflict on {string.Join(", ", ex.Entries.Select(e => e.Entity.GetType().Name))}");
        }
        catch (Exception ex) {
            return Result.Failure($"Failed to save changes: {ex.Message}");
        }
    }

}